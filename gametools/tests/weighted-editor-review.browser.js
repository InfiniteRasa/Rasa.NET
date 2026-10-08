async (page) => {
  const failures = [], consoleErrors = [], pageErrors = [];
  const onConsole = message => { if (message.type() === 'error') consoleErrors.push(message.text()); };
  const onPageError = error => pageErrors.push(error.message);
  page.on('console', onConsole);
  page.on('pageerror', onPageError);
  const check = (ok, message) => { if (!ok) failures.push(message); };
  try {
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('tab', { name: 'Save and export', exact: true }).click();
    await page.getByRole('button', { name: 'Load from server', exact: true }).click();
    await page.waitForFunction(() => document.getElementById('srv-msg').textContent.includes('Loaded'));
    await page.getByRole('tab', { name: 'Profiles and groups', exact: true }).click();
    const nativeValidity = await page.evaluate(() => ['p-key', 'g-key'].map(id => {
      const input = document.getElementById(id), original = input.value;
      const cases = ['test_1-key', 'gear-2', 'Bad Key', 'x/y', 'x|y', 'é'].map(value => {
        input.value = value;
        return { value, valid: input.checkValidity(), patternMismatch: input.validity.patternMismatch };
      });
      input.value = original;
      return { id, pattern: input.pattern, cases };
    }));
    for (const control of nativeValidity) {
      check(control.cases.slice(0, 2).every(c => c.valid), control.id + ': ASCII keys with hyphens and underscores must be natively valid.');
      check(control.cases.slice(2).every(c => !c.valid && c.patternMismatch), control.id + ': native pattern must reject invalid keys.');
    }
    check(!consoleErrors.some(message => /pattern|regular expression/i.test(message)), 'No HTML pattern console errors.');
    check(pageErrors.length === 0, 'No JavaScript page errors.');
    const receipt = { passed: failures.length === 0, failures, nativeValidity, consoleErrors, pageErrors };
    await page.evaluate(receipt => { window.__weightedReviewReceipt = receipt; }, receipt);
    return receipt;
  } finally {
    page.off('console', onConsole);
    page.off('pageerror', onPageError);
  }
}
