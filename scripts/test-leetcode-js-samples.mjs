import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';

const vaultRoot = path.resolve('interview/leetcode-hot-100-js/JavaScript复习版');

function load(relativePath, exportName) {
  const markdown = fs.readFileSync(path.join(vaultRoot, relativePath), 'utf8');
  const code = markdown.match(/```javascript\n([\s\S]*?)\n```/)?.[1];
  assert.ok(code, `${relativePath} 中没有 JavaScript 代码块`);
  const context = {};
  vm.createContext(context);
  vm.runInContext(code, context);
  assert.equal(typeof context[exportName], 'function', `${relativePath} 未定义 ${exportName}`);
  return context[exportName];
}

const twoSum = load('2-哈希/0001-两数之和.md', 'twoSum');
assert.deepEqual([...twoSum([2, 7, 11, 15], 9)], [0, 1]);

const maxArea = load('3-双指针/0011-盛最多水的容器.md', 'maxArea');
assert.equal(maxArea([1, 8, 6, 2, 5, 4, 8, 3, 7]), 49);

const lengthOfLongestSubstring = load('4-滑动窗口/0003-无重复字符的最长子串.md', 'lengthOfLongestSubstring');
assert.equal(lengthOfLongestSubstring('abcabcbb'), 3);

const coinChange = load('15-动态规划/0322-零钱兑换.md', 'coinChange');
assert.equal(coinChange([1, 2, 5], 11), 3);

const uniquePaths = load('16-多维动态规划/0062-不同路径.md', 'uniquePaths');
assert.equal(uniquePaths(3, 7), 28);

console.log('代表性用例通过：哈希、双指针、滑动窗口、动态规划、多维动态规划。');
