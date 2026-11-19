Module.preRun = Module.preRun || [];
let userDataSyncing = false;
let userDataSyncPending = false;

Module.persistUserData = function() {
    if (userDataSyncing) {
        userDataSyncPending = true;
        return;
    }

    userDataSyncing = true;
    FS.syncfs(false, function(error) {
        if (error)
            console.warn("Failed to save persistent storage!", error);
        userDataSyncing = false;
        if (userDataSyncPending) {
            userDataSyncPending = false;
            Module.persistUserData();
        }
    });
};

Module.preRun.push(function() {
    const mountPoint = "/persistent";
    FS.mkdirTree(mountPoint);

    try {
        FS.mount(IDBFS, {}, mountPoint);
    }
    catch (error) {
        console.warn("Failed to mount persistent storage!", error);
    }

    const dependency = "john-userdata";
    Module.addRunDependency(dependency);
    FS.syncfs(true, function(error) {
        if (error)
            console.warn("Failed to load persistent storage!", error);
        Module.removeRunDependency(dependency);
    });
});
