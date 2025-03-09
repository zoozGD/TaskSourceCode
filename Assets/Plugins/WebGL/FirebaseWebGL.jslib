mergeInto(LibraryManager.library, {
    // Initialize Firebase
    InitializeFirebase: function () {
        if (!window.firebase) {
            console.error("Firebase SDK not loaded.");
            return;
        }

        if (!firebase.apps.length) {
            firebase.initializeApp({
                apiKey: "AIzaSyDYgz6F3bsZg6PRdnKU1xKSuermgue5E-w",
                authDomain: "groupmanagementsystem-363fb.firebaseapp.com",
                databaseURL: "https://groupmanagementsystem-363fb-default-rtdb.asia-southeast1.firebasedatabase.app",
                projectId: "groupmanagementsystem-363fb",
                storageBucket: "groupmanagementsystem-363fb.firebasestorage.app",
                messagingSenderId: "567846780833",
                appId: "1:567846780833:web:3068eb139e9e4de34215e0"
            });

            console.log("Firebase Initialized");
        } else {
            console.log("Firebase already initialized");
        }
    },

    // ================= AUTHENTICATION ==================

    SignUpUser: function (email, password, roleIndex) {
        firebase.auth().createUserWithEmailAndPassword(UTF8ToString(email), UTF8ToString(password))
            .then((userCredential) => {
                let userId = userCredential.user.uid;
                let role = roleIndex === 0 ? "teacher" : "student";

                // Generate a unique short student ID
                let studentId = "";
                if (role === "student") {
                    studentId = "S" + Math.random().toString(36).substring(2, 4).toUpperCase() + Math.floor(10 + Math.random() * 90);
                }

                let userData = { role: role, email: UTF8ToString(email) };
                if (role === "student") {
                    userData.studentId = studentId;
                }

                firebase.database().ref('users/' + userId).set(userData)
                    .then(() => {
                        console.log(`User signed up: ${userId} | Role: ${role} | Student ID: ${studentId}`);
                        SendMessage('AuthManager', 'OnSignUpSuccess', userId);
                    })
                    .catch((error) => {
                        SendMessage('AuthManager', 'OnSignUpFailed', error.message);
                    });
            })
            .catch((error) => {
                SendMessage('AuthManager', 'OnSignUpFailed', error.message);
            });
    },

    LoginUser: function (email, password) {
        firebase.auth().signInWithEmailAndPassword(UTF8ToString(email), UTF8ToString(password))
            .then((userCredential) => {
                SendMessage('AuthManager', 'OnLoginSuccess', userCredential.user.uid);
            })
            .catch((error) => {
                SendMessage('AuthManager', 'OnLoginFailed', error.message);
            });
    },

    FetchUserRole: function (userId) {
        firebase.database().ref('users/' + UTF8ToString(userId) + '/role').once('value')
            .then((snapshot) => {
                if (snapshot.exists()) {
                    SendMessage('AuthManager', 'OnRoleFetched', snapshot.val());
                } else {
                    SendMessage('AuthManager', 'OnRoleFetchFailed', "Role not found");
                }
            })
            .catch((error) => {
                SendMessage('AuthManager', 'OnRoleFetchFailed', error.message);
            });
    },

    // ================= TEACHER DASHBOARD ==================

    CreateStudyGroupWebGL: function (name, grade, subject) {
        let groupId = firebase.database().ref('groups').push().key;
        let teacherId = firebase.auth().currentUser.uid;

        let groupData = {
            name: UTF8ToString(name),
            grade: UTF8ToString(grade),
            subject: UTF8ToString(subject),
            teacherId: teacherId,
            students: {}
        };

        firebase.database().ref('groups/' + groupId).set(groupData)
            .then(() => {
                console.log(`Group Created Successfully (ID: ${groupId})`);
                SendMessage('TeacherDashboardUI', 'LoadTeacherGroupsWebGL');
            })
            .catch(error => {
                console.error("Failed to create group:", error);
            });
    },

    LoadTeacherGroupsWebGL: function () {
        let teacherId = firebase.auth().currentUser.uid;
        firebase.database().ref('groups').orderByChild('teacherId').equalTo(teacherId).once('value')
            .then(snapshot => {
                let groups = [];
                snapshot.forEach(child => {
                    let data = child.val();
                    data.groupId = child.key;
                    groups.push(data);
                });

                SendMessage('TeacherDashboardUI', 'LoadTeacherGroups', JSON.stringify({ groups: groups }));
            })
            .catch(error => console.error("Failed to load groups:", error));
    },

    DeleteStudyGroupWebGL: function (groupId) {
        firebase.database().ref('groups/' + UTF8ToString(groupId)).remove()
            .then(() => {
                console.log(`Group deleted successfully.`);
                SendMessage('TeacherDashboardUI', 'LoadTeacherGroupsWebGL');
            })
            .catch(error => console.error("Failed to delete group:", error));
    },

    // ================= STUDENT MANAGEMENT ==================

    LoadRegisteredStudentsWebGL: function () {
        firebase.database().ref('users').orderByChild('role').equalTo("student").once('value')
            .then(snapshot => {
                let students = [];
                snapshot.forEach(child => {
                    let data = child.val();
                    students.push({ studentId: child.key, uniqueId: data.studentId || "N/A" });
                });

                SendMessage('TeacherDashboardUI', 'LoadRegisteredStudents', JSON.stringify({ students: students }));
            })
            .catch(error => console.error("Failed to load registered students:", error));
    },

    LoadAssignedStudentsWebGL: function (groupId) {
        firebase.database().ref('groups/' + UTF8ToString(groupId) + '/students').once('value')
            .then(snapshot => {
                let students = [];
                snapshot.forEach(child => {
                    students.push({ studentId: child.key, uniqueId: child.val() });
                });

                SendMessage('TeacherDashboardUI', 'LoadAssignedStudentsList', JSON.stringify({ students: students }));
            })
            .catch(error => console.error("Failed to load assigned students:", error));
    },

    AddStudentToGroupWebGL: function (groupId, studentId) {
    let studentPath = `users/${UTF8ToString(studentId)}/studentId`;
    let groupPath = `groups/${UTF8ToString(groupId)}/students/${UTF8ToString(studentId)}`;

    firebase.database().ref(studentPath).once('value')
        .then(snapshot => {
            if (snapshot.exists()) {
                let uniqueId = snapshot.val();
                console.log(`Fetched unique student ID: ${uniqueId}`);

                // Store the unique student ID instead of 'true'
                return firebase.database().ref(groupPath).set(uniqueId);
            } else {
                throw new Error(`Student unique ID not found for ${studentId}`);
            }
        })
        .then(() => {
            console.log(`Student ${studentId} added to group ${groupId}.`);
            SendMessage('TeacherDashboardUI', 'LoadAssignedStudentsWebGL', groupId);
            SendMessage('TeacherDashboardUI', 'LoadRegisteredStudentsWebGL');
        })
        .catch(error => console.error("Failed to add student:", error));
    },

    RemoveStudentFromGroupWebGL: function (groupId, studentId) {
        firebase.database().ref(`groups/${UTF8ToString(groupId)}/students/${UTF8ToString(studentId)}`).remove()
            .then(() => {
                SendMessage('TeacherDashboardUI', 'LoadAssignedStudentsWebGL', groupId);
                SendMessage('TeacherDashboardUI', 'LoadRegisteredStudentsWebGL');
            })
            .catch(error => console.error("Failed to remove student:", error));
    },

    // ================= STUDENT DASHBOARD ==================

    LoadStudentGroupsWebGL: function () {
        let studentId = firebase.auth().currentUser.uid;
        firebase.database().ref('groups').once('value')
            .then(snapshot => {
                let groups = [];
                snapshot.forEach(child => {
                    let data = child.val();
                    if (data.students && data.students.hasOwnProperty(studentId)) {
                        data.groupId = child.key;
                        data.studentCount = Object.keys(data.students).length;
                        groups.push(data);
                    }
                });

                SendMessage('StudentDashboardUI', 'LoadStudentGroups', JSON.stringify({ groups: groups }));
            })
            .catch(error => console.error("Failed to load student groups:", error));
    }
});
