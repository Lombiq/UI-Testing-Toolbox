(function gremlinSummoningCircle() {
    // Disable AMD (RequireJS/Dojo) so UMD scripts attach to window. Try catch is needed because sometimes they cause
    // errors, but it doesn't matter.
    try {
        window.define = undefined;
    }
    catch {
        try {
            if (window.define) window.define.amd = undefined;
        }
        catch {
            // Nothing to do here.
        }
    }

    try {
        window.require = undefined;
    }
    catch {
        // Nothing to do here.
    }

    try {
        window.requirejs = undefined;
    }
    catch {
        // Nothing to do here.
    }
})();
