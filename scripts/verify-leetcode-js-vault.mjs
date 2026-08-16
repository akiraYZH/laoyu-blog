import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const vaultRoot = path.resolve(process.argv[2] ?? 'interview/leetcode-hot-100-js/JavaScript复习版');
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'leetcode-js-check-'));
const failures = [];
let checked = 0;

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const fullPath = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(fullPath) : [fullPath];
  });
}

for (const markdownPath of walk(vaultRoot).filter((file) => file.endsWith('.md'))) {
  const markdown = fs.readFileSync(markdownPath, 'utf8');
  const match = markdown.match(/```javascript\n([\s\S]*?)\n```/);
  if (!match) continue;

  const tempPath = path.join(tempRoot, `${checked}.js`);
  fs.writeFileSync(tempPath, match[1]);
  try {
    execFileSync(process.execPath, ['--check', tempPath], { stdio: 'pipe' });
  } catch (error) {
    failures.push({
      file: path.relative(vaultRoot, markdownPath),
      message: error.stderr?.toString().trim() ?? error.message,
    });
  }
  checked += 1;
}

fs.rmSync(tempRoot, { recursive: true, force: true });

if (checked !== 100) failures.push({ file: '全局', message: `预期检查 100 道题，实际 ${checked} 道` });
if (failures.length > 0) {
  console.error(JSON.stringify(failures, null, 2));
  process.exit(1);
}

console.log(`验证通过：${checked} 道题的 JavaScript 代码均通过 node --check。`);
