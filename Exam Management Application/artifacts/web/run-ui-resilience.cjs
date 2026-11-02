const {chromium}=require('C:/Users/NHUT HUY/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');
const password=fs.readFileSync('.local/demo-credentials.txt','utf8').match(/^password=(.+)$/m)[1].trim();
async function main(){
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const context=await browser.newContext({viewport:{width:360,height:800},timezoneId:'Asia/Ho_Chi_Minh'});const page=await context.newPage();const result={};
 await page.goto('http://localhost:5180/Account/Login');
 await page.getByLabel('Tên đăng nhập').fill('student');
 await page.keyboard.press('Tab');result.passwordTabFocus=await page.getByLabel('Mật khẩu',{exact:true}).evaluate(el=>el===document.activeElement);
 await page.keyboard.type(password);await page.keyboard.press('Tab');result.submitTabFocus=await page.getByRole('button',{name:'Đăng nhập',exact:true}).evaluate(el=>el===document.activeElement);
 await page.keyboard.press('Enter');await page.waitForURL('**/Sessions');
 await page.getByRole('link',{name:'Xem chi tiết',exact:true}).first().click();
 page.once('dialog',d=>d.accept());const start=page.getByRole('button',{name:'Bắt đầu làm bài',exact:true});
 if(await start.count())await start.click();else await page.getByRole('link',{name:'Mở bài thi của bạn',exact:true}).click();
 await page.waitForURL(/Attempts\/(Take|Result)\/\d+/);result.attemptUrl=new URL(page.url()).pathname;
 if(page.url().includes('/Take/')){
  const card=page.locator('[data-question-id]').first();let fail=true;
  result.timezone=await page.evaluate(()=>Intl.DateTimeFormat().resolvedOptions().timeZone);
  result.deadlineUtc=await page.locator('#exam-app').getAttribute('data-deadline');
  result.remainingTime=await page.locator('#remaining-time').innerText();
  await page.route('**/Attempts/Save/**',async route=>fail?route.abort('failed'):route.continue());
  const choice=card.locator('input[type=radio]:not(:checked)').first();const selectedValue=await choice.getAttribute('value');
  await choice.focus();await page.keyboard.press('Space');
  await page.getByText('Có đáp án chưa được lưu',{exact:true}).waitFor();result.failureVisible=true;
  await page.screenshot({path:'artifacts/web/take-save-failure-mobile.png',fullPage:true});
  fail=false;await page.getByRole('button',{name:'Thử lưu lại',exact:true}).click();await page.getByText('Đã lưu tất cả thay đổi',{exact:true}).waitFor();result.retrySaved=true;
  await page.reload();result.reloadedChoice=await card.locator('input[type=radio]:checked').getAttribute('value')===selectedValue;
  await page.getByRole('button',{name:'Nộp bài',exact:true}).click();await page.getByRole('dialog').waitFor();result.dialogCancelFocus=await page.getByRole('button',{name:'Tiếp tục làm bài',exact:true}).evaluate(el=>el===document.activeElement);
  await page.keyboard.press('Escape');result.escapeClosed=await page.locator('dialog').evaluate(el=>!el.open);
 }
 fs.writeFileSync('artifacts/web/resilience-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
 await context.close();await browser.close();
}
main().catch(e=>{console.error(e.stack);process.exitCode=1});
