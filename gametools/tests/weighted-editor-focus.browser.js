async (page) => {
  const errors=[], record=e=>errors.push(e.message);
  page.on('pageerror',record);
  await page.getByRole('tab',{name:'Profiles and groups',exact:true}).click();
  await page.locator('#g-chance').fill('5000');
  await page.locator('#g-chance').dispatchEvent('change');
  const weight=page.locator('input[data-field="weight"]').first();
  await weight.fill('2'); await weight.dispatchEvent('change');
  await weight.fill('3'); await weight.dispatchEvent('change');
  await page.locator('#g-chance').focus();
  page.off('pageerror',record);
  const chance=await page.locator('#g-chance').inputValue(), value=await page.locator('input[data-field="weight"]').first().inputValue();
  return {passed:errors.length===0 && chance==='5000' && value==='3',errors,weight:value,chance};
}
