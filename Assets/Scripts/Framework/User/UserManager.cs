using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
// using Firebase;
// using Firebase.Database;
// using Firebase.Unity.Editor;
// using Firebase.Auth;
using System.IO;
using Newtonsoft.Json;

using UnityEngine.Networking;

namespace Game
{
    public class UserManager : SingletonComponent<UserManager>
    {

        #region Member Variables

		public string SaveId { get { return "user_manager"; } }

		#endregion
        
        public static string DATABASE_URL = "https://wordstones-users.firebaseio.com/";
        // FirebaseAuth auth;
        public static UserModel currentUser;
        // FirebaseUser firebaseUser;
        public CountryModel country;
        public bool userSignedup;
        public Sprite[] avatars;
        public Sprite[] avatars_sad;

        public static bool isNewUser = false;


        #region Unity Methods

	

        #endregion


        private void Start() {
            // auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            loginAnonymously();
        }
        

        public void loginAnonymously()
        {   
            
            // auth.SignInAnonymouslyAsync().ContinueWith(task => {
            // if (task.IsCanceled) {
            //     Debug.LogError("------->SignInAnonymouslyAsync was canceled.");
            //     return;
            // }
            // if (task.IsFaulted) {
            //     Debug.LogError("------->SignInAnonymouslyAsync encountered an error: " + task.Exception);
            //     return;
            // }
            // Debug.Log("------->SignInAnonymouslyAsync success " );
            // firebaseUser = task.Result;
            
            // checkCurrentUser();
            // });

            
        }

        public void signout()
        {
            // auth.SignOut();
        }

        void checkCurrentUser()
        {
            // FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(firebaseUser.UserId).GetValueAsync().ContinueWith(task => {
            //             if (task.IsFaulted) {
            //                 Debug.Log("------->no users found and got error");
            //             }
            //             else if (task.IsCompleted) {
            //             DataSnapshot snapshot = task.Result;
            //                 if (snapshot.Exists)
            //                 {   
                                
            //                     string jsonString = snapshot.GetRawJsonValue();  
            //                     //JSONObject jsonObject = new JSONObject(jsonString);        
            //                     UserModel user = JsonConvert.DeserializeObject<UserModel>(jsonString);

            //                     currentUser = user;

            //                     if(SDKManager.notificationToken != null)
            //                     {
            //                         currentUser.notificationToken = SDKManager.notificationToken;
            //                     }

            //                     SaveManager.userId = currentUser.uid;

            //                     //currentUser.levelScores = JsonConvert.DeserializeObject<Dictionary<string,float>>(jsonObject["levelScores"].ToString());
            //                     //Debug.Log("------> users received from firebase" + currentUser.uid);
            //                     //DispatcherHelper.RunOnMainThread(syncUserScores);
            //                     // if(country == null)
            //                     // {
            //                     //     DispatcherHelper.RunOnMainThread(getUserCountry);
            //                     // }
            //                 }else
            //                 {
            //                     Debug.Log("------->No users found we are creating a new one");
            //                     DispatcherHelper.RunOnMainThread(createUser);
            //                 }
            //             }
            //         });
        }

        // public void getUser(string userId,Action<UserModel> action)
        // {
        //     Debug.Log("------->getting user list for user id = " + userId);
        //     if(userId == null || userId == "")
        //     {
        //         Debug.Log("------->user id is null or empty");
        //         action(null);
        //     }else
        //     {
        //         FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(userId).GetValueAsync().ContinueWith(task => {
        //                 if (task.IsFaulted) {
        //                     Debug.Log("------->no user found and got error");
        //                 }
        //                 else if (task.IsCompleted) {
        //                 DataSnapshot snapshot = task.Result;
        //                     if (snapshot.Exists)
        //                     {    
        //                         Debug.Log("------->user found with json:" + snapshot.GetRawJsonValue());
        //                         string jsonString = snapshot.GetRawJsonValue();  
        //                         JSONObject jsonObject = new JSONObject(jsonString);        
        //                         UserModel user = JsonUtility.FromJson<UserModel>(jsonString);

                               
        //                         // if(jsonObject.HasField("levelScores"))
        //                         // {
        //                             //user.levelScores = JsonConvert.DeserializeObject<Dictionary<string,float>>(jsonObject["levelScores"].ToString());
        //                         // }
                                
        //                         Debug.Log("------> got user from firebase id = " + userId);
        //                         action(user);
                                
        //                     }else
        //                     {
        //                         Debug.Log("------->User cannot be found id = " + userId);
        //                     }
        //                 }
        //             });
        //     }
            
        // }

        void createUser()
        {
            // isNewUser = true;
            // UserModel model = new UserModel();
            // int random = Utils.RandomNumber(1,9999999);
            // string username = "gametator_" + random.ToString();
            // model.displayName = username;
            // // model.avatarId = 0;
            // model.uid = firebaseUser.UserId;

            // if(SDKManager.notificationToken != null)
            // {
            //     model.notificationToken = SDKManager.notificationToken;
            // }
            // // if(country != null)
            // // {
            // //     model.country = country.countryCode;
            // // }
            // //model.photoUrl = firebaseUser.PhotoUrl.OriginalString;
            // string json = JsonUtility.ToJson(model);
            // FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(firebaseUser.UserId).SetRawJsonValueAsync(json).ContinueWith((task => {
            //     if (task.IsFaulted) {
            //         Debug.Log("------->user cannot be created");
            //     }
            //     else if (task.IsCompleted) {
            //         Debug.Log("------->user created successfully");
            //         currentUser = model;
            //         SaveManager.userId = model.uid;
            //         //DispatcherHelper.RunOnMainThread(syncUserScores);
            //     }
            // }));

            // DispatcherHelper.RunOnMainThread(getUserCountry);
        }

        // void getUserCountry()
        // {
        //     StartCoroutine(GetRequest("https://api.ipify.org",(response)=>{
        //         string ip = response;
        //         StartCoroutine(GetRequest("http://ip-api.com/json/" + ip + "?fields=57375",(countryResponse)=>{
        //                 country = JsonUtility.FromJson<CountryModel>(countryResponse);
        //                 DispatcherHelper.RunOnMainThread(saveCountry);
        //         }));

        //     }));
            
        // }

        IEnumerator GetRequest(string uri,Action<string> response)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            Debug.Log("------>:\nReceived: " + webRequest.downloadHandler.text);
            response(webRequest.downloadHandler.text);
        }

        

        public void syncUserScores()
        {
            Dictionary<string,float> temp = new Dictionary<string, float>();

    
            
            // if(temp.Count > 0)
            // {
            //     FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(currentUser.uid).Child("levelScores").SetValueAsync(temp);
            // }

        }

        // public void updateLevelProgress(int levelId,float progress)
        // {
        //     FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(currentUser.uid).Child("levelScores").Child("level_"+ levelId.ToString()).SetValueAsync(progress);
        // }

        // public void setUserSignedUp()
        // {
        //     userSignedup = true;
        //     ES3.Save<Boolean>("userSignedup",true,data_path_user_settings);
        // }

        public void saveCountry()
        {
            if(country != null)
            {
                //ES3.Save<CountryModel>("country",country,data_path_user_settings);
                // FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(currentUser.uid).Child("country").SetValueAsync(country.countryCode);

            }
        
        }

        // public void updateUsernameAndAvatar(string name,int avatarId)
        // {
            
        //     updateUsername(name);
        //     updateAvatar(avatarId);
        // }

        // public void updateUsername(string name)
        // {
        //     DispatcherHelper.RunOnMainThread(setUserSignedUp);
        //     currentUser.displayName = name;
        //     FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(currentUser.uid).Child("displayName").SetValueAsync(name);

        // }

        // public void updateAvatar(int avatarId)
        // {
        //     DispatcherHelper.RunOnMainThread(setUserSignedUp);
        //     currentUser.avatarId = avatarId;
        //     FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").Child(currentUser.uid).Child("avatarId").SetValueAsync(avatarId);
        // }


        // public Sprite getAvatar(int id)
        // {
        //     if(avatars != null)
        //     {
        //         if(id<avatars.Length)
        //         {
        //             return avatars[id];
        //         }else
        //         {
        //             return avatars[0];
        //         }
        //     }else
        //     {
        //         return avatars[0];
        //     }
            
        // }

        // public Sprite getSadAvatar(int id)
        // {
        //     if(avatars_sad != null)
        //     {
        //         if(id<avatars_sad.Length)
        //         {
        //             return avatars_sad[id];
        //         }else
        //         {
        //             return null;
        //         }
        //     }else
        //     {
        //         return null;
        //     }
            
        // }


        // void getAllUsers()
        // {
        //     FirebaseDatabase.GetInstance(DATABASE_URL).GetReference("users").OrderByChild("levelScore/1/score").LimitToLast(1).GetValueAsync().ContinueWith(task => {
        //                 if (task.IsFaulted) {
        //                     Debug.Log("get users failed");
        //                 }
        //                 else if (task.IsCompleted) {
        //                 DataSnapshot snapshot = task.Result;
        //                     if (snapshot.Exists)
        //                     {            
        //                         Debug.Log("------> users received from firebase" + snapshot.GetRawJsonValue());
                                
        //                     }else
        //                     {
        //                         Debug.Log("No users found");
        //                     }
        //                 }
        //             });
        // }

        public void getLatestData()
        {
        }


        public void resetData()
        {
        }

        public IEnumerator ExecuteAfterTime(float time,Action task)
        {
            yield return new WaitForSeconds(time);
            task();
        }
    }
}
