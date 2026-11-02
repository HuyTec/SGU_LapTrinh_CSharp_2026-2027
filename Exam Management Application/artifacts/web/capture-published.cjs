const {chromium}=require('C:/Users/NHUT HUY/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');
const password=fs.readFileSync('.local/demo-credentials.txt','utf8').match(/^password=(.+)$/m)[1].trim();
const test=JSON.parse(fs.readFileSync('artifacts/integration/http-results.json','utf8').replace(/^\uFEFF/,''));
async function main(){
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const context=await browser.newContext({viewport:{width:1440,height:1000},timezoneId:'Asia/Ho_Chi_Minh'});const page=await context.newPage();
 await page.goto('http://localhost:5180/Account/Login');await page.getByLabel('Tên đăng nhập').fill('e2e_'+test.run);await page.getByLabel('Mật khẩu',{exact:true}).fill(password);await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();await page.waitForURL('**/Sessions');
 await page.goto('http://localhost:5180/Attempts/Result/'+test.attemptId);
 const result={score:await page.locator('.result-score').innerText(),correctCount:await page.getByText('3 / 3 câu đúng',{exact:true}).count(),correctAnswers:await page.getByText('Đáp án đúng:',{exact:true}).count()};
 await page.screenshot({path:'artifacts/web/result-published-desktop.png',fullPage:true});await page.setViewportSize({width:360,height:800});await page.screenshot({path:'artifacts/web/result-published-mobile.png',fullPage:true});
 result.mobile=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
 await page.goto('http://localhost:5180/Attempts/History');result.historyScore=await page.getByText('Điểm: 10/10',{exact:true}).count();await page.screenshot({path:'artifacts/web/history-published-mobile.png',fullPage:true});
 fs.writeFileSync('artifacts/web/published-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));await context.close();await browser.close();
}
main().catch(e=>{console.error(e.stack);process.exitCode=1});
