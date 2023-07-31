using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Realms;
using System;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Linq;

using Debug = XRDebug;
using Realms.Sync;

public static class RealmManager {

// Sync Data Config

    private static App _realmApp;
    private static User _realmUser;


    private static string _realmAppId = "carexr-sessionmaterial-gohky" ;

    private static Realm _sessionMaterial;

    public static async void EnableDeviceSync() {
        _realmApp = App.Create(new AppConfiguration(_realmAppId));
            if (_realmApp.CurrentUser == null) {
            _realmUser = await _realmApp.LogInAsync(Credentials.Anonymous());
                _sessionMaterial = await Realm.GetInstanceAsync(new PartitionSyncConfiguration(_realmUser.Id, _realmUser));

            } else {
            _realmUser = _realmApp.CurrentUser;
                _sessionMaterial = Realm.GetInstance(new PartitionSyncConfiguration(_realmUser.Id, _realmUser));

            }

        }

  

    public static async void DisableDeviceSync() {

        if (_sessionMaterial == null)
            return;

        await _realmApp.DeleteUserFromServerAsync(_realmUser);
        await _realmApp.RemoveUserAsync(_realmUser);

        await _realmUser.LogOutAsync();

        _sessionMaterial.Dispose();


      

    }

    // Embedded Config

    private static RealmConfiguration _realmConfig = new RealmConfiguration {
        SchemaVersion = 1
    };

    public static Realm Realm {
        get {
            _realmConfig.ShouldDeleteIfMigrationNeeded = true;
            return Realm.GetInstance(_realmConfig);
        }
        private set {
            Realm = value;
        }
    }


    // DB Query

    public static PanoramicSessionEntity GetPanoramicSessionData(string uuid) {
        try { 
            PanoramicSessionEntity data = _sessionMaterial.All<PanoramicSessionEntity>().Where(d => d.Uuid == uuid).FirstOrDefault();

            Debug.Log(data);
        } catch (Exception ex) {
            Debug.Log(ex.Message);

        }

        return null;
    }


    // Generic methods

    public static void BulldozeRealm() {
        using (var realm = RealmManager.Realm) {
            realm.Write(() => {
                realm.RemoveAll();

            });

            Debug.Log("The Realm was destroyed. No survivers.");

        }

    }
     /*
    public static string FindActiveUser(bool save = false) {
        List<UserEntity> userObject = RealmManager.Realm.All<UserEntity>().Where(user => user.Token != "").ToList();
        Debug.Log("User Count: " + userObject.Count);

        if (userObject.Count > 0 && save) {
            Debug.Log("WTF MEN!");
            Debug.Log("User Token Realm: " + userObject[0].Token);
            AccountManager.Token = userObject[0].Token;
            AccountManager.ActiveUserEmail = userObject[0].Email;
            return userObject[0].Token;

        }
        Debug.Log("Returning null");
        return null;
      

    }

    public static bool LogoutUser(string userEmail) {
        RealmObject userObject = RealmManager.Realm.Find<UserEntity>(userEmail);

        using (Realm Realm = RealmManager.Realm) {
            using (Transaction transaction = Realm.BeginWrite()) {
                try {
                    (userObject as UserEntity).Token = "";
                    Realm.Add(userObject, update: true);
                  
                    transaction.Commit();
                    return true;

                } catch (Exception ex) {
                    transaction.Rollback();
                    return false;

                }

            }

        }

    }


    /// <summary>
    /// Checks existance of UserEntity with the gived userEmail, if already existes just updates the content based on the Data, if not, a new one is created.
    /// </summary>
    /// <param Name="data"></param>
    /// <param Name="userEmail"></param>
    /// <returns>True: Updated/created and commited | False: Did not commit</returns>
    public static bool CreateUpdateUser(JObject data, string userEmail) {
        RealmObject userObject = RealmManager.Realm.Find<UserEntity>(userEmail);
        Debug.Log("User " + (userObject != null ? "Founded" : "Not Founded"));
        using (Realm Realm = RealmManager.Realm) {
            using (Transaction transaction = Realm.BeginWrite()) {
                try {
                    if (userObject == null) {
                        userObject = new UserEntity(
                                email: userEmail,
                                UUID: data["data"]["MemberLogin"]["uuid"].Value<string>(),
                                token: data["data"]["MemberLogin"]["token"].Value<string>()
                        );
                        Realm.Add(userObject);
                        Debug.Log("User Added");


                    } else {
                        (userObject as UserEntity).Token = data["data"]["MemberLogin"]["token"].Value<string>();
                        Realm.Add(userObject, update: true);
                        Debug.Log("User Updated");

                    }
                    transaction.Commit();
                    return true;

                } catch (Exception ex) {
                    transaction.Rollback();
                    return false;

                }

            }

        }

    }

    public static bool CreateUpdateUser(string uuid, string token, string userEmail) {
        RealmObject userObject = RealmManager.Realm.Find<UserEntity>(userEmail);
        Debug.Log("User " + (userObject != null ? "Founded" : "Not Founded"));
        using (Realm Realm = RealmManager.Realm) {
            using (Transaction transaction = Realm.BeginWrite()) {
                try {
                    if (userObject == null) {
                        userObject = new UserEntity(
                                email: userEmail,
                                UUID: uuid,
                                token: token
                        );
                        Realm.Add(userObject);
                        Debug.Log("User Added");

                    } else {
                        (userObject as UserEntity).Token = token;
                        Realm.Add(userObject, update: true);
                        Debug.Log("User Updated");

                    }
                    transaction.Commit();
                    return true;

                } catch (Exception ex) {
                    transaction.Rollback();
                    return false;

                }

            }

        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param Name="userObject"></param>
    /// <param Name="relationship"></param>
    /// <returns></returns>
    public static bool CreateUpdateUserMembership(RealmObject userObject, JToken relationship) {
        InstitutionEntity institution = RealmManager.Realm.Find<InstitutionEntity>(relationship["institution"]["uuid"].Value<string>());

        using (Realm Realm = RealmManager.Realm) {
            using (Transaction transaction = Realm.BeginWrite()) {
                try {
                    if (institution == null) {
                        institution = new InstitutionEntity(relationship["institution"]["uuid"].Value<string>());
                        RealmManager.Realm.Add(institution);

                    }

                    (userObject as UserEntity).MemberOf.Add(new MemberOf(relationship["role"].Value<string>(), institution));
                    RealmManager.Realm.Add(userObject, update: true);
                    transaction.Commit();
                    return true;

                } catch (Exception ex) {
                    Debug.Log("Error: " + ex.Message);
                    transaction.Rollback();
                    return false;

                }

            }

        }
    }

    public static bool CreateUpdateMedicationToTake(JToken data, string institutionResponsible) {
        PacientEntity pacient = RealmManager.Realm.Find<PacientEntity>(data["pacient"]["uuid"].Value<string>());
        if (pacient is null)
            pacient = new PacientEntity(data["pacient"]["uuid"].Value<string>(), RealmManager.Realm.Find<InstitutionEntity>(institutionResponsible));

        MedicationEntity medication = RealmManager.Realm.Find<MedicationEntity>(data["medication"]["uuid"].Value<string>());
        if (medication is null)
            medication = new MedicationEntity(data["medication"]["uuid"].Value<string>(), data["medication"]["Name"].Value<string>());

        MedicationToTakeEntity medicationToTake = null;
        medicationToTake = RealmManager.Realm.All<MedicationToTakeEntity>().Filter(
                "Medication.UUID == '" + medication.UUID + "' && Pacient.UUID == '" + pacient.UUID + "'"
                ).FirstOrDefault();

        if (medicationToTake is null) {
            if (data["atTime"].Type != JTokenType.Null)
                medicationToTake = new MedicationToTakeEntity(data["quantity"].Value<byte>(), data["timeMeasure"].Value<string>(), data["intOfTime"].Value<int>(), DateTimeOffset.Parse(data["atTime"].Value<string>()), pacient, medication);
            else
                medicationToTake = new MedicationToTakeEntity(data["quantity"].Value<byte>(), data["timeMeasure"].Value<string>(), data["intOfTime"].Value<int>(), pacient, medication);

        } else {
            if (medicationToTake.AtTime > DateTimeOffset.Parse(data["atTime"].Value<string>())) {
                // TO DO (When we start to have mutations in the API XD

            }

        }

        using (Realm Realm = RealmManager.Realm) {
            using (Transaction transaction = Realm.BeginWrite()) {
                try {
                    Realm.Add(pacient, update: true);
                    Realm.Add(medication, update: true);
                    Realm.Add(medicationToTake, update: true);

                    transaction.Commit();
                    return true;

                } catch (Exception ex) {
                    Debug.Log(ex.Message);
                    transaction.Rollback();
                    return false;

                }

            }

        }

    }*/

}
