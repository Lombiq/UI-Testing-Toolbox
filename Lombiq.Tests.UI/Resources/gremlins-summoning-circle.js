(function () {
    // Disable AMD (RequireJS/Dojo) so UMD scripts attach to window. Try catch is needed because sometimes they cause
    // errors, but it doesn't matter.
    try
    {
        window.define = undefined;
    }
    catch (_)
    {
        try
        {
            if (window.define) window.define.amd = undefined;
        }
        catch (_)
        { }
    }

    try
    {
        window.require = undefined;
    }
    catch (_)
    { }

    try
    {
        window.requirejs = undefined;
    }
    catch (_)
    { }
})();
