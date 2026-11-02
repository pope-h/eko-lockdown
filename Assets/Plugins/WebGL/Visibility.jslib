// Silence the game whenever its page is not on screen: another tab, a minimised window,
// or leaving the page. Unity stops its own loop when hidden, but Web Audio would keep playing.
mergeInto(LibraryManager.library, {
  EkoWatchVisibility: function () {
    if (typeof document === 'undefined') return;
    var suspendedByUs = false;
    function ctx() { return (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) ? WEBAudio.audioContext : null; }
    function hide() { var c = ctx(); if (c && c.state === 'running') { c.suspend(); suspendedByUs = true; } }
    function show() { var c = ctx(); if (c && suspendedByUs) { c.resume(); suspendedByUs = false; } }
    document.addEventListener('visibilitychange', function () { if (document.hidden) hide(); else show(); });
    window.addEventListener('pagehide', hide);
    window.addEventListener('blur', hide);
    window.addEventListener('focus', show);
  }
});
