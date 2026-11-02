const {chromium}=require('C:/Users/NHUT HUY/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');const password=fs.readFileSync('.local/demo-credentials.txt','utf8').match(/^password=(.+)$/m)[1].trim();
async function main(){
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});const context=await browser.newContext({viewport:{width:360,height:800},timezoneId:'Asia/Ho_Chi_Minh'});const page=await context.newPage();const result={types:{},created:[]};const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://localhost:5180/Account/Register');
 for(const type of ['Student','Lecturer','ExamOfficer','Administrator']){
  await page.getByLabel('Loại tài khoản',{exact:true}).selectOption(type);result.types[type]=await page.locator('#StudentCode').evaluate(el=>({required:el.required,disabled:el.disabled,visible:!el.closest('#student-code-field').hidden}));
 }
 await page.screenshot({path:'artifacts/web/register-staff-mobile.png',fullPage:true});result.mobile=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'artifacts/web/register-staff-desktop.png',fullPage:true});
 for(const type of ['Lecturer','Administrator']){
  await page.goto('http://localhost:5180/Account/Register');await page.getByLabel('Loại tài khoản',{exact:true}).selectOption(type);
  const userName='staff_'+type.toLowerCase()+'_'+Date.now().toString(36);
  await page.getByLabel('Tên đăng nhập',{exact:true}).fill(userName);await page.getByLabel('Họ và tên',{exact:true}).fill('Kiểm thử đăng ký '+type);await page.getByLabel('Email',{exact:true}).fill(userName+'@example.test');await page.getByLabel('Mật khẩu',{exact:true}).fill(password);await page.getByLabel('Nhập lại mật khẩu',{exact:true}).fill(password);await page.getByRole('button',{name:'Đăng ký tài khoản',exact:true}).click();await page.waitForURL('**/Account/Login');
  const notice=await page.locator('[role=status]').allInnerTexts();result.created.push({type,userName,notice});await page.setViewportSize({width:360,height:800});await page.screenshot({path:'artifacts/web/register-'+type.toLowerCase()+'-pending-mobile.png',fullPage:true});
 }
 result.pageErrors=errors;fs.writeFileSync('artifacts/web/staff-registration-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));await context.close();await browser.close();
}
main().catch(e=>{console.error(e.stack);process.exitCode=1});
