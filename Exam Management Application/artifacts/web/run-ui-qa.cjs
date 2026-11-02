const { chromium } = require('C:/Users/NHUT HUY/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const password = fs.readFileSync('.local/demo-credentials.txt','utf8').match(/^password=(.+)$/m)[1].trim();
const out = path.resolve('artifacts/web');
async function main() {
 const browser = await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const context = await browser.newContext({viewport:{width:1440,height:1000},timezoneId:'Asia/Ho_Chi_Minh'});
 const page = await context.newPage(); const errors=[]; const failed=[]; const result={};
 page.on('pageerror',e=>errors.push(e.message));
 page.on('response',r=>{if(r.status()>=400)failed.push(r.status()+' '+new URL(r.url()).pathname)});
 const screenshot = async name => page.screenshot({path:path.join(out,name+'.png'),fullPage:true});
 const overflow = async () => page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
 await page.goto('http://localhost:5180/Account/Login');
 await screenshot('login-desktop'); await page.setViewportSize({width:360,height:800}); await screenshot('login-mobile'); result.loginMobile=await overflow();
 await page.getByLabel('Tên đăng nhập').fill('student2'); await page.getByLabel('Mật khẩu',{exact:true}).fill(password);
 await page.getByRole('button',{name:'Đăng nhập',exact:true}).click(); await page.waitForURL('**/Sessions');
 await screenshot('sessions-mobile'); result.sessionsMobile=await overflow();
 await page.getByLabel('Tên đợt thi hoặc môn học').fill('zzzz-no-results'); await page.getByRole('button',{name:'Tìm kiếm',exact:true}).click();
 await page.getByText('Chưa có đợt thi phù hợp',{exact:true}).waitFor(); result.emptySearch=true; await screenshot('sessions-empty-mobile');
 await page.goto('http://localhost:5180/Sessions'); await page.setViewportSize({width:1440,height:1000}); await screenshot('sessions-desktop');
 result.sessionText=(await page.locator('.session-list').innerText()).slice(0,700);
 await page.getByRole('link',{name:'Xem chi tiết',exact:true}).first().click(); await screenshot('details-desktop');
 await page.setViewportSize({width:360,height:800}); await screenshot('details-mobile'); result.detailsMobile=await overflow();
 page.once('dialog',d=>d.accept());
 const start=page.getByRole('button',{name:'Bắt đầu làm bài',exact:true});
 if(await start.count()) await start.click(); else await page.getByRole('link',{name:'Mở bài thi của bạn',exact:true}).click();
 await page.waitForURL(/Attempts\/(Take|Result)\/\d+/);
 result.attemptUrl=new URL(page.url()).pathname;
 if(page.url().includes('/Take/')) {
   const card=page.locator('[data-question-id]').first();
   await card.getByRole('radio').first().check(); await card.getByRole('checkbox').check();
   await page.getByText('Đã lưu tất cả thay đổi',{exact:true}).waitFor();
   result.saved=true; await screenshot('take-mobile'); result.takeMobile=await overflow();
   await page.reload(); await card.getByRole('radio').first().waitFor(); result.restored=await card.getByRole('radio').first().isChecked()&&await card.getByRole('checkbox').isChecked();
   await page.setViewportSize({width:1440,height:1000}); await screenshot('take-desktop');
   // Hold the first save long enough to change the same question again while it is in flight.
   let saves=0;
   await page.route('**/Attempts/Save/**',async route=>{saves++; if(saves===1)await new Promise(r=>setTimeout(r,500));await route.continue();});
   const options=card.getByRole('radio');
   await options.nth(1).check(); await options.nth(0).check();
   await page.getByRole('button',{name:'Nộp bài',exact:true}).click();
   await page.getByRole('button',{name:'Xác nhận nộp bài',exact:true}).click();
   await page.waitForURL('**/Attempts/Result/*'); result.queuedSaves=saves; result.submitted=true;
 }
 await screenshot('result-desktop'); await page.setViewportSize({width:360,height:800}); await screenshot('result-mobile'); result.resultMobile=await overflow();
 result.unpublished=await page.getByText(/Kết quả chưa được công bố/).count()>0; result.scoreHidden=await page.locator('.result-score').count()===0;
 await page.goto('http://localhost:5180/Attempts/History'); await screenshot('history-mobile'); result.historyMobile=await overflow(); await page.setViewportSize({width:1440,height:1000}); await screenshot('history-desktop');
 await page.goto('http://localhost:5180/'); await screenshot('home-desktop'); await page.setViewportSize({width:360,height:800}); await screenshot('home-mobile'); result.homeMobile=await overflow();
 result.pageErrors=errors; result.failedResponses=failed;
 fs.writeFileSync(path.join(out,'qa-results.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify(result,null,2)); await browser.close();
}
main().catch(e=>{console.error(e.stack);process.exitCode=1});
