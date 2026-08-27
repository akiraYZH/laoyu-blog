# Docker 与部署（R091～R100）

## R091. 容器里的 API 为什么连不上 `localhost:5432`？
**口述：** 容器内 localhost 是自己；Compose 服务间用 service name，例如 `Host=postgres`，宿主机才用映射端口。  
**追问/失分点：** container port 与 host port 区别。  
**英文口述：** Inside a container, `localhost` refers to that container. Compose services use the service DNS name, such as `Host=postgres`, while the host uses the published port.  
**英文追问回答：** The container port is where the service listens inside the network; the host port is the external mapping and can be different.

## R092. Dockerfile 为什么使用 multi-stage build？
**口述：** SDK 阶段 build/publish，runtime 阶段只带运行产物，减小镜像和攻击面。  
**追问/失分点：** restore 层如何利用缓存？  
**英文口述：** A multi-stage Dockerfile uses an SDK stage to restore, build, and publish, then copies only published output into a runtime image. This reduces size and attack surface.  
**英文追问回答：** Copying project files and restoring before frequently changing source files lets Docker reuse the dependency cache.

## R093. `depends_on` 是否保证数据库可用？
**口述：** 启动顺序不等于 ready；需要 healthcheck、`service_healthy` 或应用侧有限重试。  
**追问/失分点：** 不用固定 sleep 猜启动时间。  
**英文口述：** `depends_on` controls startup order but does not by itself prove that the database is ready. I use a health check with an appropriate condition or bounded application retries.  
**英文追问回答：** A fixed sleep guesses timing and can be both unnecessarily slow and unreliable on a slower machine.

## R094. 配置和 secret 怎样传进容器？
**口述：** 非敏感配置可用环境变量；secret 用平台 secret store 或受保护挂载，不写进镜像和仓库。  
**追问/失分点：** 泄露后必须轮换。  
**英文口述：** I externalize non-sensitive settings through runtime configuration and store secrets in a protected platform store or mounted secret. Secrets never enter the Dockerfile, image, or repository.  
**英文追问回答：** If a secret is exposed, removing the file is not enough; I rotate the credential and review its usage.

## R095. 容器为什么启动后立刻退出？
**口述：** 查看 exit code 和 logs，检查入口命令、配置、架构和依赖；容器随主进程退出是正常模型。  
**追问/失分点：** 不先盲目重建所有镜像。  
**英文口述：** I start with the container exit code and logs, then check the entry command, required configuration, platform architecture, and dependency availability. A container exits when its main process exits.  
**英文追问回答：** Blindly rebuilding every image destroys useful diagnostic focus and may not change the underlying configuration failure.

## R096. Kestrel 前为什么常放 reverse proxy？
**口述：** 代理或负载均衡处理公共入口、TLS、路由和流量策略；应用要配置 forwarded headers。  
**追问/失分点：** Kestrel 也能对外，不说“必须 IIS”。  
**英文口述：** A reverse proxy or load balancer commonly provides the public endpoint, TLS termination, routing, and traffic policy in front of Kestrel. The app must handle forwarded headers correctly.  
**英文追问回答：** Kestrel can be internet-facing, so IIS is not a universal requirement; the deployment architecture determines the choice.

## R097. 零停机部署需要考虑什么？
**口述：** readiness、滚动替换、优雅停止及新旧版本兼容；数据库采用 expand-contract 变更。  
**追问/失分点：** 先删列再部署新代码会怎样？  
**英文口述：** Zero-downtime deployment needs readiness checks, rolling replacement, graceful shutdown, and compatibility while old and new versions overlap. Database changes follow expand-and-contract.  
**英文追问回答：** Dropping a column before all old code is retired can break requests still served by the previous version.

## R098. 单台 EC2 与 ECS 怎么选？
**口述：** 小项目先单机控制成本；出现高可用、扩缩容和发布需求后再评估 ECS/ALB/RDS。  
**追问/失分点：** 不把复杂等同于专业。  
**英文口述：** For a small project I start with one EC2 instance and Docker Compose to control cost and complexity. I evaluate ECS, ALB, and RDS when availability, scaling, or deployment needs justify them.  
**英文追问回答：** A more complex cloud architecture is not automatically more professional; it must solve a measured requirement.

## R099. 生产 migration 在哪里执行？
**口述：** 由受控部署 job/script 单独执行并审查，避免多实例启动时竞争；准备备份与回滚方案。  
**追问/失分点：** rollback 不一定恢复已删除数据。  
**英文口述：** I run production migrations as one controlled, reviewed deployment job rather than letting multiple app instances race at startup. I prepare backups and a rollback or forward-fix plan.  
**英文追问回答：** Rolling back schema code cannot necessarily recover data that a destructive migration already deleted.

## R100. 部署成功后如何确认服务真的可用？
**口述：** 检查容器、readiness、日志和指标，再从外部 smoke test 关键 endpoint、数据库与认证路径。  
**追问/失分点：** 进程运行不等于服务可用。  
**英文口述：** I check container state, readiness, logs, and metrics, then run external smoke tests through critical endpoints, including database and authentication paths. A running process alone is not proof of service.  
**英文追问回答：** I define smoke-test expectations, rollback thresholds, and alerts before deployment so failure has a clear response.

