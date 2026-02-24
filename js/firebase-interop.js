let app = null;
let auth = null;
let db = null;

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
            db = firebase.firestore();
        }
        return true;
    },

    // Firestore operations

    async getTodos() {
        const timeout = new Promise((_, reject) =>
            setTimeout(() => reject(new Error(
                "Firestore request timed out. Is Cloud Firestore enabled in your Firebase project?"
            )), 10000)
        );
        const query = db.collection("todos").orderBy("order").get().then((snapshot) =>
            snapshot.docs.map((doc) => ({ id: doc.id, ...doc.data() }))
        );
        return Promise.race([query, timeout]);
    },

    async addTodo(todo) {
        const docRef = await db.collection("todos").add(todo);
        return docRef.id;
    },

    async updateTodo(docId, data) {
        await db.collection("todos").doc(docId).update(data);
    },

    async deleteTodo(docId) {
        await db.collection("todos").doc(docId).delete();
    },

    async reorderTodos(updates) {
        const batch = db.batch();
        for (const u of updates) {
            batch.update(db.collection("todos").doc(u.id), {
                order: u.order,
            });
        }
        await batch.commit();
    },

    // Group operations

    async getGroups() {
        const timeout = new Promise((_, reject) =>
            setTimeout(() => reject(new Error(
                "Firestore request timed out. Is Cloud Firestore enabled in your Firebase project?"
            )), 10000)
        );
        const query = db.collection("groups").orderBy("order").get().then((snapshot) =>
            snapshot.docs.map((doc) => ({ id: doc.id, ...doc.data() }))
        );
        return Promise.race([query, timeout]);
    },

    async addGroup(group) {
        const docRef = await db.collection("groups").add(group);
        return docRef.id;
    },

    async updateGroup(docId, data) {
        await db.collection("groups").doc(docId).update(data);
    },

    async deleteGroup(docId) {
        await db.collection("groups").doc(docId).delete();
    },

    async deleteGroupWithUngroup(groupId, todoIds) {
        const batch = db.batch();
        for (const id of todoIds) {
            batch.update(db.collection("todos").doc(id), { groupId: null });
        }
        batch.delete(db.collection("groups").doc(groupId));
        await batch.commit();
    },

    // Settings (key-value store)
    async getSetting(key) {
        const timeout = new Promise((_, reject) => setTimeout(() => reject(new Error("Firestore timeout")), 10000));
        const query = db.collection("settings").doc(key).get().then((doc) => {
            return doc.exists ? doc.data().value : null;
        });
        return Promise.race([query, timeout]);
    },

    async setSetting(key, value) {
        await db.collection("settings").doc(key).set({ value: value });
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

    // Returns a promise that resolves once Firebase has loaded persisted auth state
    waitForAuthState() {
        const self = this;
        return new Promise((resolve) => {
            if (!auth) {
                resolve(null);
                return;
            }
            const unsubscribe = auth.onAuthStateChanged((user) => {
                unsubscribe();
                resolve(self._serializeUser(user));
            });
        });
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
