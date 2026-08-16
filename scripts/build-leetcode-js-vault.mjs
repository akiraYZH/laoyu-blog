import fs from 'node:fs';
import path from 'node:path';

const projectRoot = path.resolve('interview/leetcode-hot-100-js');
const referenceRoot = '/tmp/leetcode-javascript-reference/solutions';
const outputRoot = path.join(projectRoot, 'JavaScript复习版');

const pathMetadata = new Map(Object.entries({
  '10-回溯/1-0046-全排列.md': ['0046', '全排列'],
  '10-回溯/2-.md': ['0078', '子集'],
  '10-回溯/3.md': ['0017', '电话号码的字母组合'],
  '10-回溯/4.md': ['0039', '组合总和'],
  '10-回溯/5.md': ['0022', '括号生成'],
  '10-回溯/6.md': ['0079', '单词搜索'],
  '10-回溯/7.md': ['0131', '分割回文串'],
  '10-回溯/8.md': ['0051', 'N皇后'],
  '11-二分查找/1.md': ['0035', '搜索插入位置'],
  '11-二分查找/2.md': ['0074', '搜索二维矩阵'],
  '11-二分查找/3.md': ['0034', '在排序数组中查找元素的第一个和最后一个位置'],
  '11-二分查找/4.md': ['0033', '搜索旋转排序数组'],
  '11-二分查找/5.md': ['0153', '寻找旋转排序数组中的最小值'],
  '11-二分查找/6.md': ['0004', '寻找两个正序数组的中位数'],
  '12-栈/1.md': ['0020', '有效的括号'],
  '12-栈/2.md': ['0155', '最小栈'],
  '12-栈/3.md': ['0394', '字符串解码'],
  '12-栈/4.md': ['0739', '每日温度'],
  '12-栈/5.md': ['0084', '柱状图中最大的矩形'],
  '13-堆/1.md': ['0215', '数组中的第K个最大元素'],
  '13-堆/2.md': ['0347', '前K个高频元素'],
  '13-堆/3.md': ['0295', '数据流的中位数'],
  '14-贪心算法/1.md': ['0121', '买卖股票的最佳时机'],
  '14-贪心算法/2.md': ['0055', '跳跃游戏'],
  '14-贪心算法/3.md': ['0045', '跳跃游戏II'],
  '14-贪心算法/4.md': ['0763', '划分字母区间'],
  '15-动态规划/1.md': ['0070', '爬楼梯'],
  '15-动态规划/2.md': ['0118', '杨辉三角'],
  '15-动态规划/3.md': ['0198', '打家劫舍'],
  '15-动态规划/4.md': ['0279', '完全平方数'],
  '15-动态规划/5.md': ['0322', '零钱兑换'],
  '15-动态规划/6.md': ['0139', '单词拆分'],
  '15-动态规划/7.md': ['0300', '最长递增子序列'],
  '15-动态规划/8.md': ['0152', '乘积最大子数组'],
  '15-动态规划/9.md': ['0416', '分割等和子集'],
  '15-动态规划/10.md': ['0032', '最长有效括号'],
  '16-多维动态规划/1.md': ['0062', '不同路径'],
  '16-多维动态规划/2.md': ['0064', '最小路径和'],
  '16-多维动态规划/3.md': ['0005', '最长回文子串'],
  '16-多维动态规划/4.md': ['1143', '最长公共子序列'],
  '16-多维动态规划/5.md': ['0072', '编辑距离'],
  '17-技巧/1.md': ['0136', '只出现一次的数字'],
  '17-技巧/2.md': ['0169', '多数元素'],
  '17-技巧/3.md': ['0075', '颜色分类'],
  '17-技巧/4.md': ['0031', '下一个排列'],
  '17-技巧/5.md': ['0287', '寻找重复数'],
  '2-哈希/3-0129-最长连续序列.md': ['0128', '最长连续序列'],
}));

const categories = fs.readdirSync(projectRoot, { withFileTypes: true })
  .filter((entry) => entry.isDirectory() && /^\d+-/.test(entry.name))
  .map((entry) => entry.name)
  .filter((name) => name !== '1-目录')
  .sort((a, b) => Number.parseInt(a) - Number.parseInt(b));

const referenceFiles = fs.readdirSync(referenceRoot);
const cards = [];

function metadataFor(relativePath) {
  if (pathMetadata.has(relativePath)) return pathMetadata.get(relativePath);
  const fileName = path.basename(relativePath, '.md');
  const match = fileName.match(/-(\d{4})-(.+)$/);
  if (!match) throw new Error(`无法识别题号: ${relativePath}`);
  return [match[1], match[2]];
}

function solutionFor(id) {
  const fileName = referenceFiles.find((name) => name.startsWith(`${id}-`) && name.endsWith('.js'));
  if (!fileName) throw new Error(`缺少 JavaScript 参考实现: ${id}`);
  const raw = fs.readFileSync(path.join(referenceRoot, fileName), 'utf8').trim();
  const url = raw.match(/https:\/\/leetcode\.com\/problems\/[^\s*]+/)?.[0] ?? `https://leetcode.com/problemset/?search=${Number(id)}`;
  const code = raw.replace(/^\/\*\*[\s\S]*?\*\/\s*/, '').trim();
  return { code, url, sourceFile: fileName };
}

fs.rmSync(outputRoot, { recursive: true, force: true });
fs.mkdirSync(outputRoot, { recursive: true });

for (const category of categories) {
  const sourceCategory = path.join(projectRoot, category);
  const targetCategory = path.join(outputRoot, category);
  fs.mkdirSync(targetCategory, { recursive: true });

  const files = fs.readdirSync(sourceCategory).filter((name) => name.endsWith('.md')).sort();
  for (const file of files) {
    const relativePath = `${category}/${file}`;
    const [id, title] = metadataFor(relativePath);
    const { code, url, sourceFile } = solutionFor(id);
    const cardName = `${id}-${title}.md`;
    const card = `---\n题号: ${Number(id)}\n分类: ${category.replace(/^\d+-/, '')}\n状态: 未开始\ntags:\n  - LeetCode\n  - JavaScript\n---\n\n# ${Number(id)}. ${title}\n\n- [ ] 第一次独立完成\n- [ ] 不看答案写出\n- [ ] 1 天后复习\n- [ ] 7 天后复习\n- [ ] 30 天后复习\n\n[打开 LeetCode](${url})\n\n## JavaScript 解法\n\n\`\`\`javascript\n${code}\n\`\`\`\n\n## 我的思路\n\n> 用自己的话写出核心思路。\n\n## 容易出错的地方\n\n- \n\n## 复杂度\n\n- 时间复杂度：\n- 空间复杂度：\n`;
    fs.writeFileSync(path.join(targetCategory, cardName), card);
    cards.push({ id, title, category, cardName, sourceFile });
  }
}

cards.sort((a, b) => Number(a.id) - Number(b.id));
const categorySections = categories.map((category) => {
  const categoryCards = cards.filter((card) => card.category === category);
  const links = categoryCards.map((card) => `- [ ] [[${category}/${card.cardName.replace(/\.md$/, '')}|${Number(card.id)}. ${card.title}]]`).join('\n');
  return `## ${category.replace(/^\d+-/, '')}\n\n${links}`;
}).join('\n\n');

const home = `# LeetCode Hot 100 · JavaScript 复习版\n\n> 共 ${cards.length} 道题。建议先做题，再看解法；看懂不等于会写。\n\n## 推荐复习节奏\n\n1. 第一次：独立思考 20～30 分钟。\n2. 第二次：看思路后关闭答案，重新写一遍。\n3. 按 1 天、7 天、30 天复习并勾选。\n4. “我的思路”和“容易出错的地方”必须自己填写。\n\n${categorySections}\n`;
fs.writeFileSync(path.join(outputRoot, '00-首页与进度.md'), home);

const readme = `# 使用说明\n\n这是可独立使用的 Obsidian JavaScript 复习库，不包含原仓库的 Java 题解。\n\n- 用 Obsidian 打开本目录。\n- 从 \`00-首页与进度.md\` 开始。\n- 每道题都有 JavaScript 解法、复习勾选项和个人笔记区。\n- 代码参考 Josh Crozier 的 MIT 许可项目，并按原项目题号重新整理。\n\n## iPad + Mac 同步\n\n将解压后的 \`LeetCode-Hot100-JavaScript\` 文件夹放到 iCloud Drive 的 Obsidian 文件夹中，再分别用 Mac 和 iPad 的 Obsidian 打开。首次同步完成后可以离线阅读。\n`;
fs.writeFileSync(path.join(outputRoot, 'README.md'), readme);

const license = fs.readFileSync('/tmp/leetcode-javascript-reference/LICENSE', 'utf8').trim();
fs.writeFileSync(path.join(outputRoot, 'THIRD_PARTY_NOTICES.md'), `# 第三方代码说明\n\nJavaScript 参考实现整理自：\n\n- https://github.com/JoshCrozier/leetcode-javascript\n- 对应文件记录在生成清单中。\n\n## MIT License\n\n\`\`\`text\n${license}\n\`\`\`\n`);
fs.writeFileSync(path.join(outputRoot, '生成清单.json'), `${JSON.stringify(cards, null, 2)}\n`);

console.log(`已生成 ${cards.length} 道 JavaScript 复习卡片: ${outputRoot}`);
