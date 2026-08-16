import fs from 'node:fs';
import path from 'node:path';

const sourceRoot = path.resolve('interview/leetcode-hot-100-js');
const referenceRoot = '/tmp/leetcode-javascript-reference/solutions';
const outputRoot = path.resolve('interview/LeetCode-Hot100-JavaScript-完整讲解版');

const specialMetadata = new Map(Object.entries({
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

const referenceFiles = fs.readdirSync(referenceRoot);

function metadataFor(relativePath) {
  if (specialMetadata.has(relativePath)) return specialMetadata.get(relativePath);
  const match = path.basename(relativePath, '.md').match(/-(\d{4})-(.+)$/);
  if (!match) throw new Error(`无法识别题号: ${relativePath}`);
  return [match[1], match[2]];
}

function solutionFor(id) {
  const fileName = referenceFiles.find((name) => name.startsWith(`${id}-`) && name.endsWith('.js'));
  if (!fileName) throw new Error(`缺少 JavaScript 参考实现: ${id}`);
  const raw = fs.readFileSync(path.join(referenceRoot, fileName), 'utf8').trim();
  const url = raw.match(/https:\/\/leetcode\.com\/problems\/[^\s*]+/)?.[0] ?? `https://leetcode.com/problemset/?search=${Number(id)}`;
  const code = raw.replace(/^\/\*\*[\s\S]*?\*\/\s*/, '').trim();
  return { code, url };
}

function insertAfterTitle(markdown, addition) {
  const firstLineEnd = markdown.indexOf('\n');
  if (firstLineEnd === -1) return `${markdown}\n\n${addition}\n`;
  return `${markdown.slice(0, firstLineEnd)}\n\n${addition}\n${markdown.slice(firstLineEnd + 1)}`;
}

fs.rmSync(outputRoot, { recursive: true, force: true });
fs.mkdirSync(outputRoot, { recursive: true });

const categories = fs.readdirSync(sourceRoot, { withFileTypes: true })
  .filter((entry) => entry.isDirectory() && /^\d+-/.test(entry.name) && entry.name !== '1-目录')
  .map((entry) => entry.name)
  .sort((a, b) => Number.parseInt(a) - Number.parseInt(b));

const generated = [];
for (const category of categories) {
  fs.mkdirSync(path.join(outputRoot, category), { recursive: true });
  const files = fs.readdirSync(path.join(sourceRoot, category)).filter((name) => name.endsWith('.md')).sort();

  for (const file of files) {
    const relativePath = `${category}/${file}`;
    const [id, title] = metadataFor(relativePath);
    const { code, url } = solutionFor(id);
    const original = fs.readFileSync(path.join(sourceRoot, relativePath), 'utf8');
    let removedBlocks = 0;
    const proseOnly = original.replace(/```java\s*\n[\s\S]*?```/g, () => {
      removedBlocks += 1;
      return '> 本处原 Java 示例已移除；请使用本文开头的 JavaScript 背诵版答案。';
    });
    if (removedBlocks === 0) throw new Error(`${relativePath} 没有找到 Java 代码块`);

    const answer = `## JavaScript 背诵版答案\n\n- [ ] 第一次独立完成\n- [ ] 不看答案写出\n- [ ] 1 天后复习\n- [ ] 7 天后复习\n- [ ] 30 天后复习\n\n[打开 LeetCode](${url})\n\n\`\`\`javascript\n${code}\n\`\`\`\n\n> 下方完整保留原题解的中文说明；原文中的 Java 示例统一指向上面的 JavaScript 最终答案。`;
    const transformed = insertAfterTitle(proseOnly, answer)
      .replaceAll('Java代码', 'JavaScript 代码')
      .replaceAll('Java 代码', 'JavaScript 代码')
      .replaceAll('Java实现', 'JavaScript 实现')
      .replaceAll('Java 实现', 'JavaScript 实现');

    fs.writeFileSync(path.join(outputRoot, category, file), transformed);
    generated.push({ id, title, category, file, removedBlocks });
  }
}

if (fs.existsSync(path.join(sourceRoot, 'assets'))) {
  fs.cpSync(path.join(sourceRoot, 'assets'), path.join(outputRoot, 'assets'), { recursive: true });
}

const sections = categories.map((category) => {
  const links = generated
    .filter((item) => item.category === category)
    .map((item) => `- [ ] [[${category}/${item.file.replace(/\.md$/, '')}|${Number(item.id)}. ${item.title}]]`)
    .join('\n');
  return `## ${category.replace(/^\d+-/, '')}\n\n${links}`;
}).join('\n\n');

const readme = `# LeetCode Hot 100 · JavaScript 完整讲解版\n\n> 保留原项目的完整中文解释，并为每题提供一份可提交、适合背诵的 JavaScript 答案。\n\n## 使用方法\n\n1. 从下面目录选择题目。\n2. 先读中文解释，理解思路。\n3. 回到文章开头背诵 JavaScript 答案。\n4. 按 1 天、7 天、30 天进行复习。\n\n${sections}\n`;
fs.writeFileSync(path.join(outputRoot, '00-首页与进度.md'), readme);
fs.writeFileSync(path.join(outputRoot, 'README.md'), `${readme}\n## 说明\n\n原文中的 Java 代码块已移除，每篇文章开头提供统一的 JavaScript 最终答案。原项目：https://github.com/ninjaAlgorithm/LeetCode-Solutions-Hot-100\n`);

const license = fs.readFileSync('/tmp/leetcode-javascript-reference/LICENSE', 'utf8').trim();
fs.writeFileSync(path.join(outputRoot, 'THIRD_PARTY_NOTICES.md'), `# 第三方代码说明\n\nJavaScript 答案整理自 https://github.com/JoshCrozier/leetcode-javascript\n\n\`\`\`text\n${license}\n\`\`\`\n`);
fs.writeFileSync(path.join(outputRoot, '生成清单.json'), `${JSON.stringify(generated, null, 2)}\n`);

console.log(`已生成 ${generated.length} 篇完整中文讲解，共替换 ${generated.reduce((sum, item) => sum + item.removedBlocks, 0)} 个 Java 代码块。`);
