let app = null;
let auth = null;

window.firebaseInterop = {
    hasConfig() {
        return localStorage.getItem("firebase_config") !== null;
    },

    getConfig() {
        const raw = localStorage.getItem("firebase_config");
        return raw ? JSON.parse(raw) : null;
    },

    setConfig(configJson) {
        localStorage.setItem("firebase_config", configJson);
    },

    clearConfig() {
        localStorage.removeItem("firebase_config");
    },

    initialize() {
        const config = this.getConfig();
        if (!config) return false;
        if (!app) {
            app = firebase.initializeApp(config);
            auth = firebase.auth();
        }
        return true;
    },

    async signInWithGitHub() {
        const provider = new firebase.auth.GithubAuthProvider();
        provider.addScope("repo");
        const result = await auth.signInWithPopup(provider);
        const credential =
            firebase.auth.GithubAuthProvider.credentialFromResult(result);
        if (credential?.accessToken) {
            sessionStorage.setItem("github_token", credential.accessToken);
        }
        return this._serializeUser(result.user);
    },

    async signOut() {
        await auth.signOut();
        sessionStorage.removeItem("github_token");
    },

    getCurrentUser() {
        if (!auth) return null;
        return this._serializeUser(auth.currentUser);
    },

    getGitHubToken() {
        return sessionStorage.getItem("github_token");
    },

    onAuthStateChanged(dotNetRef) {
        if (!auth) return;
        auth.onAuthStateChanged((user) => {
            dotNetRef.invokeMethodAsync(
                "OnAuthStateChanged",
                this._serializeUser(user),
            );
        });
    },

    _serializeUser(user) {
        if (!user) return null;
        return {
            uid: user.uid,
            email: user.email,
            displayName: user.displayName,
            photoURL: user.photoURL,
        };
    },
};
