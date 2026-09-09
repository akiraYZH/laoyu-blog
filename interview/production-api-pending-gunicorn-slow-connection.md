# Production API 一直 Pending：Gunicorn 单 Worker 被慢连接占满

> 一次真实的、由 AI 辅助排查的 Production Incident 笔记

## 1. 事故概述

OldFish production 出现以下现象：

- 前端页面可以打开；
- 所有 `/api/v1/...` 请求长时间显示 `Pending`；
- 重启后端容器后可以暂时恢复；
- 过一段时间又会再次卡死；
- 服务器 CPU 和内存占用都很低。

最终定位到的核心问题是：

```text
Docker 将 Gunicorn 的 8000 端口发布到了所有公网网卡
  ↓
互联网扫描器绕过 Cloudflare，直接连接 VPS:8000
  ↓
其中一个连接没有发送完整 HTTP 请求
  ↓
唯一的 Gunicorn sync worker 阻塞在 socket.recv()
  ↓
正常 API 请求没有可用 worker，只能等待
  ↓
浏览器显示 Pending
```

这不是数据库卡死，也不是 CPU 或内存不足。

## 2. 正常请求链路

正常用户不会直接访问 Gunicorn，而是经过以下链路：

```text
浏览器
  ↓
https://www.oldfish.ca/api/v1/...
  ↓
Cloudflare Worker
  ↓
Workers VPC / Cloudflare Tunnel
  ↓
后端私网地址:8000
  ↓
Gunicorn
  ↓
Django
  ↓
PostgreSQL 或外部市场数据服务
```

这里的职责边界是：

- Cloudflare：公网入口、TLS、流量过滤和网络防护；
- Tunnel/VPC：把 Cloudflare 请求安全地送到私网 Origin；
- Gunicorn：运行 Python WSGI 应用；
- Django：处理 HTTP 路由和业务逻辑；
- PostgreSQL：持久化数据。

## 3. 为什么扫描器可以绕过 Cloudflare

事故发生时，production Compose 使用：

```yaml
ports:
  - "8000:8000"
```

没有指定 Host IP 时，Docker 会把端口发布到所有宿主机接口，效果近似于：

```text
0.0.0.0:8000
```

因此除了 Cloudflare 私网，公网也可以直接访问：

```text
http://<VPS_PUBLIC_IP>:8000
```

异常请求不需要经过 `oldfish.ca`，所以会绕过 Cloudflare 的入口保护。

日志中出现了与 OldFish 业务完全无关的路径，例如：

```text
/query?q=SHOW+DIAGNOSTICS
/solr/admin/info/system
/v2/_catalog
/cgi-bin/authLogin.cgi
```

这更像是自动化互联网扫描，不足以证明有人专门针对 OldFish 发起攻击。

## 4. TCP、HTTP 和端口是什么关系

可以用快递做类比：

- IP 地址：大楼地址；
- 端口：大楼里的房间号；
- TCP：规定如何建立可靠连接、按顺序发送数据；
- HTTP：TCP 连接里承载的具体 Web 请求内容。

关系如下：

```text
HTTP 请求
装在 TCP 连接中
通过 IP 地址和端口到达 Gunicorn
```

Gunicorn 的 HTTP/1.1 服务使用 TCP，因此防火墙规则需要匹配：

```text
Protocol: TCP
Destination port: 8000
```

UDP 不是这次 Gunicorn 请求的传输入口。

## 5. 为什么一个慢连接能让全部 API Pending

修复前的 Gunicorn 命令没有配置 `--workers`：

```yaml
command: gunicorn config.wsgi:application \
  --bind 0.0.0.0:8000 \
  --timeout 120
```

Gunicorn 默认使用 `sync` worker。一个 sync worker 同一时间只处理一个请求，而当时只有一个 worker。

正常客户端会快速发送完整请求：

```http
GET /api/v1/market-status/ HTTP/1.1
Host: www.oldfish.ca
```

异常客户端可以这样做：

```text
建立 TCP 连接
  ↓
只发送部分请求，或者暂时什么都不发送
  ↓
不主动关闭连接
```

Gunicorn 已经把唯一 worker 分配给该连接，只能等待剩余请求内容。

日志中的决定性证据是：

```text
WORKER TIMEOUT
Error handling request (no URI read)
...
data = unreader.read()
...
self.sock.recv(...)
```

`no URI read` 表示 Gunicorn 连完整的请求 URI 都没有读到。请求尚未进入 Django 路由或数据库查询阶段。

此时正常请求的结果是：

```text
Cloudflare 收到正常 API 请求
  ↓
VPC fetch 请求后端 8000
  ↓
唯一 worker 正在等待异常客户端
  ↓
正常请求排队
  ↓
Cloudflare 等不到首包
  ↓
浏览器显示 Pending
```

## 6. 为什么 CPU 很低，服务却卡死

Worker 卡在网络读取：

```python
socket.recv(...)
```

它在等待客户端继续发送数据，而不是进行 CPU 计算。因此可能同时出现：

```text
CPU 很低
内存很低
API 完全不可用
```

所以“CPU 不高”只能排除 CPU 饱和，不能证明服务健康。

## 7. 为什么重启只能暂时恢复

执行容器重启后：

```text
旧 worker 和异常连接被关闭
  ↓
Gunicorn 启动新 worker
  ↓
积压的正常请求重新得到处理
```

但是公网 8000 仍然开放时，下一个扫描连接还能再次占住新 worker。

因此：

```text
重启 = 临时恢复
关闭公网入口 = 根因修复
```

## 8. `DisallowedHost` 为什么不是直接根因

部分扫描器发送了完整 HTTP 请求，但使用服务器 IP 作为 Host：

```text
Host: <VPS_PUBLIC_IP>:8000
```

Django 使用 `ALLOWED_HOSTS` 验证 Host。服务器 IP 不在允许名单中，因此抛出：

```text
django.core.exceptions.DisallowedHost
```

这是 Django 正确的安全行为。不能为了消除日志而把公网 IP 加进 `ALLOWED_HOSTS`，否则会正式允许通过服务器 IP 绕过域名访问 Django。

需要区分两类请求：

```text
完整请求 + 非法 Host
→ 进入 Django
→ 被 ALLOWED_HOSTS 拒绝

不完整慢请求
→ 卡在 Gunicorn HTTP parser/socket.recv()
→ 尚未进入 Django
→ 直接占住 worker
```

因此 `DisallowedHost` 是公网暴露的证据，但 `no URI read` 才是这次 Pending 的关键证据。

## 9. 为什么 Production 意外开启了 DEBUG

原来的 Django 配置：

```python
DEBUG = os.environ.get("DEBUG", "True") == "True"
```

这表示环境变量缺失时默认开启 Debug。

Production workflow 每次部署都会重新生成 `.env`，但原流程没有写入 `DEBUG`：

```text
部署重新生成 .env
  ↓
.env 中没有 DEBUG
  ↓
settings.py 使用默认值 True
  ↓
Production 开启 Debug
```

这导致非法请求得到很大的详细错误页面，并可能暴露文件路径、配置结构和运行环境信息。

`DEBUG=True` 不是慢连接 Pending 的直接根因，但它放大了信息泄露、日志噪声和错误响应成本。

## 10. 代码修复

### 10.1 Django 默认安全关闭 Debug

修复后：

```python
DEBUG = os.environ.get("DEBUG", "False") == "True"
```

即使部署环境漏掉变量，也不会意外开启 Production Debug。

### 10.2 Production workflow 显式写入 Debug

部署流程增加：

```bash
printf 'DEBUG="False"\n' >> .env.tmp
```

现在形成双保险：

```text
部署流程明确设置 DEBUG=False
              +
代码默认值也是 False
```

### 10.3 Gunicorn 增加到两个 worker

修复后：

```yaml
command: gunicorn config.wsgi:application \
  --bind 0.0.0.0:8000 \
  --workers 2 \
  --timeout 120 \
  --access-logfile - \
  --error-logfile -
```

两个 worker 意味着一个请求变慢时，另一个 worker 仍有机会处理请求。

但这只是可用性缓冲，不是安全边界。只增加 worker 而继续公开 8000，攻击者仍可能同时占满所有 worker。

## 11. 网络层修复

### 11.1 当前已验证的宿主机防护

在 Docker 的 `DOCKER-USER` 链中：

```text
已建立连接                         → ACCEPT
Cloudflare 私网来源访问 TCP 8000   → ACCEPT
其他来源访问 TCP 8000              → DROP
```

这组规则已经保存为 Debian 的持久化 iptables 规则。

验证结果：

```text
https://www.oldfish.ca/api/... → HTTP 200
http://<VPS_PUBLIC_IP>:8000    → 连接超时
```

### 11.2 OVH Edge Firewall

更外层的防护是在 OVH Edge Network Firewall 中拒绝：

```text
Mode: Refuse
Protocol: TCP
Source IP: Any
Destination port: 8000
TCP status: None
```

它会在恶意流量到达 VPS 之前丢弃请求。

> 当前笔记不能把 OVH Edge 规则写成已完成：必须在规则保存、启用，并从外部重新验证后，才能标记为已验证。

推荐的纵深防御是：

```text
公网扫描器
  ↓
OVH Edge Firewall：拒绝公网 TCP 8000
  ↓
宿主机 DOCKER-USER：再次限制 TCP 8000
  ↓
Docker
  ↓
Gunicorn
```

## 12. 最终验证

本次已经验证：

- Production API 返回 `200`；
- 公网 IP 的 TCP 8000 无法连接；
- Cloudflare Tunnel 请求仍然正常；
- Gunicorn 启动两个不同 PID 的 worker；
- Django `DEBUG=False`；
- GitHub Actions Production deployment 成功。

验证 Gunicorn worker：

```bash
docker logs --since 5m personal_site_backend 2>&1 \
  | grep "Booting worker"
```

验证 Django Debug：

```bash
docker exec personal_site_backend \
  python manage.py shell -c \
  "from django.conf import settings; print('DEBUG=', settings.DEBUG)"
```

验证外部 API：

```bash
curl -i --max-time 15 \
  https://www.oldfish.ca/api/v1/market-status/
```

## 13. 根因与修复的对应关系

| 问题 | 证据 | 修复 |
|---|---|---|
| 公网可直接连接 Gunicorn | 多个公网扫描 IP 直接出现在 Gunicorn 日志 | OVH Edge + `DOCKER-USER` 阻断公网 TCP 8000 |
| 唯一 sync worker 被占住 | `WORKER TIMEOUT`、`no URI read`、`socket.recv()` | 阻止慢客户端直连，增加为 2 个 worker |
| Production 意外开启 Debug | 详细 traceback、约 67 KB 错误响应、运行时 `DEBUG=True` | 代码默认 False，workflow 显式写入 False |
| 重启后问题复发 | 新 worker 启动后短暂恢复，随后再次 Pending | 不再把重启当根因修复 |
| CPU 很低但 API 不可用 | CPU/内存正常，worker 阻塞在网络读取 | 同时检查请求链路、日志、首包时间和 worker 状态 |

## 14. 面试 STAR 回答

### Situation

Production 前端页面可以正常打开，但所有后端 API 请求持续 Pending。重启后短暂恢复，随后再次发生。

### Task

需要在不误判数据库或 Cloudflare 的前提下快速恢复服务，定位根因并降低复发风险。

### Action

我按照请求链路分层测试 Cloudflare 页面、Tunnel/Nginx 80 端口和 Gunicorn 8000 端口。结果表明只有 Gunicorn 路径超时。

服务器 CPU 和内存都很低，但日志出现 `WORKER TIMEOUT` 和 `no URI read`。结合 Gunicorn sync worker 模型，我确认唯一 worker 正在等待一个不完整的公网 TCP 请求，请求甚至还没有进入 Django。

我先重启容器恢复服务，然后阻止公网直接访问 8000，只保留 Cloudflare 私网入口。同时将 Gunicorn worker 从一个增加到两个。

排查中还发现 Production workflow 没有写入 `DEBUG`，而代码默认值为 `True`。我将默认值改为 `False`，并在部署流程中显式生成 `DEBUG=False`。

### Result

Production API 恢复为 `200`，公网 IP 无法直接连接 8000，Gunicorn 启动两个 worker，Django Debug 关闭。问题不再依赖反复重启恢复。

## 15. 中文口述版

> 我处理过一次 Production API 持续 Pending 的问题。前端页面正常，但所有 `/api/v1` 请求没有响应。我先按请求链路分层测试，确认 Cloudflare 和 Tunnel 的 Web 路径正常，只有 Gunicorn 的 8000 端口超时。服务器 CPU 和内存很低，但 Gunicorn 日志出现了 `WORKER TIMEOUT` 和 `no URI read`。进一步检查发现，Docker 将 8000 发布到了公网，而 Gunicorn 当时只有一个 sync worker。扫描器建立 TCP 连接却不发送完整请求，导致唯一 worker 阻塞在 `socket.recv()`，正常请求只能排队。我先重启恢复服务，然后在网络层阻止公网直接访问 8000，只保留 Cloudflare 私网入口，并增加到两个 worker。我还发现部署流程漏掉了 `DEBUG`，代码又默认开启 Debug，因此同时修复了 Production Debug 配置。最后我通过外部 API、端口测试、容器日志和 Django runtime settings 验证了修复。

## 16. English Interview Answer

> I handled a production incident where the frontend was available, but all `/api/v1` requests remained pending. I tested the request path layer by layer and found that the Cloudflare frontend and the Tunnel path to Nginx were responsive, while requests to the Gunicorn service on port 8000 timed out. CPU and memory usage were low, but the Gunicorn logs showed `WORKER TIMEOUT` and `no URI read`. The decisive finding was that Docker had published port 8000 publicly and Gunicorn was running a single synchronous worker. An automated scanner could open a TCP connection without sending a complete HTTP request, leaving the only worker blocked in `socket.recv()` before the request reached Django. I restored service, restricted public access to port 8000 while keeping the Cloudflare private path available, and increased Gunicorn to two workers. I also found that the production deployment recreated the environment file without a `DEBUG` value while Django defaulted it to `True`, so I changed the safe default and explicitly deployed `DEBUG=False`. I verified the result through external API checks, blocked direct-port tests, container logs, and runtime Django settings.

## 17. 常见追问

### 为什么 CPU 很低但 API 仍然卡死？

因为 worker 在等待网络数据，不是在进行 CPU 计算。阻塞在 `socket.recv()` 几乎不消耗 CPU，但会占用 worker。

### `DisallowedHost` 是根因吗？

不是。它说明完整的非法 Host 请求进入 Django 后被正确拒绝。真正造成 Pending 的请求没有发送完整 URI，卡在 Gunicorn HTTP parser 中。

### 为什么不把服务器 IP 加进 `ALLOWED_HOSTS`？

因为扫描器不应该通过公网 IP 访问 Django。把 IP 加入允许名单只会放宽入口，不能解决慢连接。

### 为什么不增加 `--timeout`？

增加 timeout 会允许异常连接占住 worker 更长时间。应该关闭公网入口，而不是让 Gunicorn 等得更久。

### 为什么两个 worker 还不够？

两个 worker 只能增加容错。只要公网入口开放，攻击者仍可能占满所有 worker。网络隔离才是根本边界。

### 为什么公网不应该直接访问 Gunicorn？

Gunicorn 是应用服务器，不是公网安全入口。Cloudflare或反向代理应该处理 TLS、慢客户端、过滤和流量策略，再把可信请求交给 Gunicorn。

### 为什么 CPU、内存和容器 Running 都不能证明服务健康？

进程可以处于运行状态，但所有 worker 都在等待网络、外部 API 或数据库。必须验证真实请求、首包时间和关键依赖。

### 为什么需要同时修改代码默认值和部署流程？

部署流程显式配置可以表达 Production 意图，安全默认值可以防止未来再次遗漏。两层同时存在才能避免单点配置错误。

## 18. AI 参与应该怎样诚实表达

这次排障由 AI 协助提出假设、生成命令并解释日志。面试时不应声称所有步骤都是独立完成的。

可以诚实表达为：

> 我使用 AI 作为排障助手来生成假设和诊断命令，但我根据真实 Production 日志逐项验证。我执行了服务器检查、审查了代码修改、完成部署，并通过内外两条请求链路确认结果。我也会质疑不合适的建议，例如直接修改服务器 `.env`，并将它改为本地代码修改后通过正式流程部署。

英文表达：

> I used an AI coding assistant to generate diagnostic hypotheses and commands, but I validated each hypothesis against real production evidence. I executed the server checks, reviewed the code changes, deployed them through the normal workflow, and verified the result from both internal and external paths. I also challenged suggestions that did not fit the deployment model, such as treating a direct server environment edit as the permanent fix.

## 19. 面试中的失分点

- 把自动扫描直接描述成“有人专门攻击我的网站”；
- 把 `DisallowedHost` 当成 Pending 的直接根因；
- 看到 CPU 很低就认定服务器健康；
- 只靠重启或增加 worker，不关闭公网入口；
- 为消除错误而把服务器 IP 加入 `ALLOWED_HOSTS`；
- 声称 OVH Edge Firewall 已验证，但实际还没有完成启用和外部测试；
- 隐瞒 AI 的关键参与，或背诵自己无法解释的命令；
- 把 Production 部署成功等同于服务已经完成运行时验证。

## 20. 我应该真正掌握的六个问题

1. 为什么 CPU 很低但 API 仍会卡死？
2. `DisallowedHost` 为什么不是 Pending 的直接原因？
3. 为什么重启只能暂时恢复？
4. 为什么只增加 worker 不够？
5. 为什么公网不应该直接访问 Gunicorn？
6. `DEBUG=True` 是怎样被部署流程意外开启的？

能够不用背稿、用自己的话回答这六个问题，才算真正把这次 AI 辅助排障转化成自己的工程经验。
