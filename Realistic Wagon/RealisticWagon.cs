// Project:         Realistic Wagon mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallConnect.Utility;
using System.Collections.Generic;

namespace RealisticWagon
{

    [FullSerializer.fsObject("v1")]
    public class RealisticWagonSaveData
    {
        public string ModVersion;
        public DFPosition WagonMapPixel;
        public bool WagonDeployed;
        public Vector3 WagonPosition;
        public Quaternion WagonRotation;
        public Matrix4x4 WagonMatrix;
        public DFPosition HorseMapPixel;
        public bool HorseDeployed;
        public Vector3 HorsePosition;
        public Quaternion HorseRotation;
        public Matrix4x4 HorseMatrix;
        public string HorseName;
        public DFPosition CurrentMapPixel;
        public bool WagonClose;
    }


    public class RealisticWagon : MonoBehaviour, IHasModSaveData
    {
        private static string ModVersion;

        private static DFPosition WagonMapPixel = null;
        private static bool WagonDeployed = false;
        private static Vector3 WagonPosition;
        private static Quaternion WagonRotation;
        private static GameObject Wagon = null;
        private static Matrix4x4 WagonMatrix;

        private static DFPosition HorseMapPixel = null;
        private static bool HorseDeployed = false;
        private static Vector3 HorsePosition;
        private static Quaternion HorseRotation;
        private static GameObject Horse = null;
        private static Matrix4x4 HorseMatrix;
        private static string HorseName;
        private static DFPosition CurrentMapPixel;

        private static bool WagonClose;
        private static bool NeedToGround;

        public Type SaveDataType
        {
            get { return typeof(RealisticWagonSaveData); }
        }

        public object NewSaveData()
        {
            return new RealisticWagonSaveData
            {
                ModVersion = mod.ModInfo.ModVersion,
                WagonMapPixel = new DFPosition(),
                WagonDeployed = false,
                WagonPosition = new Vector3(),
                WagonRotation = new Quaternion(),
                WagonMatrix = new Matrix4x4(),
                HorseMapPixel = new DFPosition(),
                HorseDeployed = false,
                HorsePosition = new Vector3(),
                HorseRotation = new Quaternion(),
                HorseMatrix = new Matrix4x4(),
                HorseName = "",
                CurrentMapPixel = new DFPosition(),
                WagonClose = false
            };
        }

        public object GetSaveData()
        {
            return new RealisticWagonSaveData
            {
                ModVersion = mod.ModInfo.ModVersion,
                WagonMapPixel = WagonMapPixel,
                WagonDeployed = WagonDeployed,
                WagonPosition = WagonPosition,
                WagonRotation = WagonRotation,
                WagonMatrix = WagonMatrix,
                HorseMapPixel = HorseMapPixel,
                HorseDeployed = HorseDeployed,
                HorsePosition = HorsePosition,
                HorseRotation = HorseRotation,
                HorseMatrix = HorseMatrix,
                HorseName = HorseName,
                CurrentMapPixel = CurrentMapPixel,
                WagonClose = WagonClose
            };
        }

        public void RestoreSaveData(object saveData)
        {
            RealisticWagonSaveData realisticWagonSaveData = (RealisticWagonSaveData)saveData;
            WagonMapPixel = realisticWagonSaveData.WagonMapPixel;
            WagonDeployed = realisticWagonSaveData.WagonDeployed;
            WagonPosition = realisticWagonSaveData.WagonPosition;
            WagonRotation = realisticWagonSaveData.WagonRotation;
            WagonMatrix = realisticWagonSaveData.WagonMatrix;
            HorseMapPixel = realisticWagonSaveData.HorseMapPixel;
            HorseDeployed = realisticWagonSaveData.HorseDeployed;
            HorsePosition = realisticWagonSaveData.HorsePosition;
            HorseRotation = realisticWagonSaveData.HorseRotation;
            HorseMatrix = realisticWagonSaveData.HorseMatrix;
            HorseName = realisticWagonSaveData.HorseName;
            CurrentMapPixel = realisticWagonSaveData.CurrentMapPixel;
            WagonClose = realisticWagonSaveData.WagonClose;

            DestroyWagon();
            if (WagonDeployed)
            {
                PlaceWagon(true);
            }
            DestroyHorse();
            if (HorseDeployed)
            {
                PlaceHorse(true);
            }
        }
        

        static Mod mod;
        static RealisticWagon instance;

        public const int wagonModelID = 41241;

        static DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static TransportManager transportManager = GameManager.Instance.TransportManager;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RealisticWagon>();
            instance = go.AddComponent<RealisticWagon>();
            mod.SaveDataInterface = instance;

            PlayerActivate.RegisterCustomActivation(mod, 41241, MountWagon);
            PlayerActivate.RegisterCustomActivation(mod, 201, 0, MountHorse);

            PlayerEnterExit.OnPreTransition += OnPreInterior_PlaceMounts;
            PlayerEnterExit.OnTransitionInterior += OnTransitionInterior_GiveTempWagon;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_DropTempWagon;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_AdjustTransport;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_InventoryCleanup;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_HeightExitCorrection;
            PlayerEnterExit.OnTransitionDungeonInterior += OnTransitionInterior_GiveTempWagon;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_DropTempWagon;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_AdjustTransport;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_InventoryCleanup;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_HeightExitCorrection;

            ModVersion = mod.ModInfo.ModVersion;
            mod.IsReady = true;
        }

        private void Update()
        {
            if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused || DaggerfallUI.Instance.FadeBehaviour.FadeInProgress)
                return;

            if (transportManager.IsOnShip())
            {
                return;
            }

            if (NeedToGround)
            {
                if (HorseDeployed)
                {
                    PlaceHorseOnGround();
                    Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);
                }
                if (WagonDeployed)
                {
                    PlaceWagonOnGround();
                    Wagon.transform.SetPositionAndRotation(WagonPosition, WagonRotation);
                }
                NeedToGround = false;
            }

            if ((HorseName == "" || HorseName == null) && transportManager.HasHorse())
            {
                NameHorse();
            }

            if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside)
            {
                //If you have a wagon but no horse, the wagon is dropped.
                if (!transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart)
                {
                    transportManager.TransportMode = TransportModes.Foot;
                    PlaceWagon();
                }
                //If you have wagon and horse, but change to Foot, horse and wagon is dropped.
                else if (transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
                {
                    LeaveHorseWagon();
                }
                //If you have wagon and horse, but change to Horse, wagon is dropped.
                else if (transportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Horse)
                {
                    LeaveWagon();
                }
                //If you have horse, but change to foot, horse is dropped.
                else if (transportManager.HasHorse() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
                {
                    LeaveHorse();
                }
            }

            if (!GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && transportManager.HasCart())
            {
                if (transportManager.HasCart())
                {
                    LeaveWagon();
                }
                if (transportManager.HasHorse())
                {
                    LeaveHorse();
                }
                transportManager.TransportMode = TransportModes.Foot;
            }
            

            if (transportManager.HasCart() && WagonDeployed && !playerEnterExit.IsPlayerInsideDungeon && !WagonClose)
            {
                NewWagonPopUp();
            }
            if (transportManager.HasHorse() && HorseDeployed)
            {
                DestroyHorse();
                HorseDeployed = false;
                HorseName = "";
            }
        }

        private static void OnPreInterior_PlaceMounts(PlayerEnterExit.TransitionEventArgs args)
        {
            CurrentMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;

            if (!playerEnterExit.IsPlayerInside)
            {
                if (transportManager.HasCart())
                {
                    LeaveWagon();
                }
                if (transportManager.HasHorse())
                {
                    LeaveHorse();
                }
                transportManager.TransportMode = TransportModes.Foot;
            }
        }
        

        private static void OnTransitionInterior_GiveTempWagon(PlayerEnterExit.TransitionEventArgs args)
        {
            if (WagonDeployed && CurrentMapPixel.Y == WagonMapPixel.Y && CurrentMapPixel.X == WagonMapPixel.X)
            {
                WagonClose = true;
                DaggerfallUnityItem wagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                wagon.value = 0;
                GameManager.Instance.PlayerEntity.Items.AddItem(wagon);
            }
        }

        private static void OnTransitionExterior_DropTempWagon(PlayerEnterExit.TransitionEventArgs args)
        {
            WagonClose = false;
            if (WagonDeployed)
            {
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                    {
                        playerItems.RemoveItem(item);
                    }
                }
            }
        }

        private static void OnTransitionExterior_AdjustTransport(PlayerEnterExit.TransitionEventArgs args)
        {
            if (transportManager.IsOnShip())
            {
                return;
            }
            else if (transportManager.HasCart() && transportManager.TransportMode != TransportModes.Cart)
            {
                transportManager.TransportMode = TransportModes.Cart;
            }
            else if (transportManager.HasHorse() && transportManager.TransportMode != TransportModes.Horse)
            {
                transportManager.TransportMode = TransportModes.Horse;
            }

            DestroyWagon();
            if (WagonDeployed)
            {
                PlaceWagon(true);
            }
            DestroyHorse();
            if (HorseDeployed)
            {
                PlaceHorse(true);
            }
        }

        private static void OnTransitionExterior_InventoryCleanup(PlayerEnterExit.TransitionEventArgs args)
        {
            //Code to make sure the player only has one horse and one wagon.
            DaggerfallUnityItem newHorse = null;
            DaggerfallUnityItem newWagon = null;
            List<DaggerfallUnityItem> wagons = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Transportation, (int)Transportation.Small_cart);
            foreach (DaggerfallUnityItem small_cart in wagons)
            {
                newWagon = small_cart;
                GameManager.Instance.PlayerEntity.Items.RemoveItem(small_cart);
            }
            List<DaggerfallUnityItem> horses = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Transportation, (int)Transportation.Horse);
            foreach (DaggerfallUnityItem horse in horses)
            {
                newHorse = horse;
                GameManager.Instance.PlayerEntity.Items.RemoveItem(horse);
            }
            if (newHorse != null)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(newHorse);
            }
            if (newWagon != null)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(newWagon);
            }
        }

        private static void LeaveHorseWagon()
        {
            if (!RoomBehind())
            {
                DaggerfallUI.MessageBox("There is not enough room for your wagon behind you.");
                transportManager.TransportMode = TransportModes.Cart;
            }
            else
            {
                LeaveWagon();
                LeaveHorse();
            }
        }



        private static void NewWagonPopUp()
        {
            DaggerfallMessageBox newWagonPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            string[] message = { "Do you wish to abandon your old wagon and start using your new wagon?" };
            newWagonPopup.SetText(message);
            newWagonPopup.OnButtonClick += NewWagonPopup_OnButtonClick;
            newWagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            newWagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            newWagonPopup.Show();
        }

        private static void NewWagonPopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                DestroyWagon();
                WagonDeployed = false;
                GameManager.Instance.PlayerEntity.WagonItems.Clear();
            }
            else
            {
                sender.CloseWindow();
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                    {
                        playerItems.RemoveItem(item);
                    }
                }
            }
        }


        //Wagon code

        private static void LeaveWagon()
        {
            if (!RoomBehind())
            {
                DaggerfallUI.MessageBox("There is not enough room for your wagon behind you.");
                transportManager.TransportMode = TransportModes.Cart;
            }
            else
            {
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                    {
                        playerItems.RemoveItem(item);
                    }
                }
                PlaceWagon();
            }
        }

        private static void PlaceWagon(bool fromSave = false)
        {
            if (fromSave == false)
            {
                WagonMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                if (!transportManager.HasHorse())
                {
                    SetWagonPositionAndRotation(true);
                    DaggerfallUI.MessageBox("You have no horse to pull your wagon.");
                    ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                    for (int i = 0; i < playerItems.Count; i++)
                    {
                        DaggerfallUnityItem item = playerItems.GetItem(i);
                        if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                        {
                            playerItems.RemoveItem(item);
                        }
                    }
                }
                else
                {
                    SetWagonPositionAndRotation();
                    DaggerfallUI.MessageBox("You unhitch your wagon.");
                }
            }
            else
            {
                PlaceWagonOnGround();
            }
            Wagon = MeshReplacement.ImportCustomGameobject(wagonModelID, null, WagonMatrix);
            if (Wagon == null)
            {
                Wagon = GameObjectHelper.CreateDaggerfallMeshGameObject(wagonModelID, null);
            }

            Wagon.transform.SetPositionAndRotation(WagonPosition, WagonRotation);
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                Wagon.SetActive(false);
            }
            else
            {
                Wagon.SetActive(true);
            }
            WagonDeployed = true;
        }

        private static void PlaceWagonOnGround()
        {
            RaycastHit hit;
            Ray ray = new Ray(WagonPosition, Vector3.down);
            if (Physics.Raycast(ray, out hit, 1000))
            {
                WagonPosition = hit.point + hit.transform.up;
                WagonRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                Ray rayUp = new Ray(WagonPosition + (Vector3.up * 500), Vector3.down);
                if (Physics.Raycast(rayUp, out hit, 1000))
                {
                    WagonPosition = hit.point + (hit.transform.up * 1.1f);
                    WagonRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }
        }

        private static void SetWagonPositionAndRotation(bool front = false)
        {
            GameObject player = GameManager.Instance.PlayerObject;
            float position = 4;
            if (front) { position = -3; }
            WagonPosition = player.transform.position - (player.transform.forward * position);
            WagonMatrix = player.transform.localToWorldMatrix;

            RaycastHit hit;
            Ray ray = new Ray(WagonPosition, Vector3.down);
            if (Physics.Raycast(ray, out hit, 10))
            {
                WagonPosition = hit.point + hit.transform.up;
                WagonRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }        

        private static void MountWagon(RaycastHit hit)
        {
            DaggerfallMessageBox wagonPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            if (hit.transform.gameObject.GetInstanceID() == Wagon.GetInstanceID())
            {
                if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Info)
                {
                    DaggerfallUI.AddHUDText("You see your wagon");
                }
                else if (!transportManager.HasHorse())
                {
                    DaggerfallUI.MessageBox("You have no horse to pull your wagon.");
                }
                else if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.MessageBox("You are unable to levitate your wagon.");
                }
                else
                {
                    DaggerfallUnityItem WagonItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                    DestroyWagon();
                    GameManager.Instance.PlayerEntity.Items.AddItem(WagonItem);
                    WagonDeployed = false;
                    WagonMatrix = new Matrix4x4();
                    transportManager.TransportMode = TransportModes.Cart;
                    DaggerfallUI.MessageBox("You hitch your wagon.");                   
                }
            }
            else
            {
                DaggerfallUI.MessageBox("This is not your wagon.");
            }
        }


        //Horse code

        private static void LeaveHorse()
        {
            if (!RoomBehind())
            {
                DaggerfallUI.MessageBox("There is not enough room for " + HorseName + " behind you.");
                transportManager.TransportMode = TransportModes.Horse;
            }
            else
            {
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Horse))
                    {
                        playerItems.RemoveItem(item);
                    }
                }
                PlaceHorse();
            }
        }

        private static void PlaceHorse(bool fromSave = false)
        {
            if (fromSave == false)
            {
                HorseMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                SetHorsePositionAndRotation();
                DaggerfallUI.MessageBox("You dismount " + HorseName + ".");
            }
            else
            {
                PlaceHorseOnGround();
            }
            //GameObject HorseBill = MeshReplacement.ImportCustomFlatGameobject(538, 1, HorsePosition, null);
            Horse = GameObjectHelper.CreateDaggerfallBillboardGameObject(201, 0, null);

            Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                Horse.SetActive(false);
            }
            else
            {
                Horse.SetActive(true);
            }
            AddHorseAudioSource(Horse);
            HorseDeployed = true;
        }

        private static void PlaceHorseOnGround()
        {
            RaycastHit hit;
            Ray rayDown = new Ray(HorsePosition, Vector3.down);
            if (Physics.Raycast(rayDown, out hit, 1000))
            {
                HorsePosition = hit.point + (hit.transform.up * 1.1f);
                HorseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                Ray rayUp = new Ray(HorsePosition + (Vector3.up * 500), Vector3.down);
                if (Physics.Raycast(rayUp, out hit, 1000))
                {
                    HorsePosition = hit.point + (hit.transform.up * 1.1f);
                    HorseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }
        }

        private static void SetHorsePositionAndRotation()
        {
            GameObject player = GameManager.Instance.PlayerObject;
            HorsePosition = player.transform.position - (player.transform.forward * 1.5f);
            HorseMatrix = player.transform.localToWorldMatrix;

            RaycastHit hit;
            Ray ray = new Ray(HorsePosition, Vector3.down);
            if (Physics.Raycast(ray, out hit, 10))
            {
                HorsePosition = hit.point + (hit.transform.up * 1.1f);
                HorseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                Debug.Log("Setting tent position and rotation failed");
            }
        }

        private static void MountHorse(RaycastHit hit)
        {
            DaggerfallMessageBox horsePopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            if (hit.transform.gameObject.GetInstanceID() == Horse.GetInstanceID())
            {
                if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Info)
                {
                    DaggerfallUI.AddHUDText("You see "+HorseName);
                }
                else if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.MessageBox(HorseName+" is unable to levitate.");
                }
                else
                {
                    DaggerfallUnityItem HorseItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Horse);
                    DestroyHorse();
                    GameManager.Instance.PlayerEntity.Items.AddItem(HorseItem);
                    HorseDeployed = false;
                    HorseMatrix = new Matrix4x4();
                    transportManager.TransportMode = TransportModes.Horse;
                    DaggerfallUI.MessageBox("You mount " + HorseName +".");                    
                }
            }
            else
            {
                DaggerfallUI.MessageBox("This is not your "+HorseName+".");
            }
        }

        private static void OnTransitionExterior_HeightExitCorrection(PlayerEnterExit.TransitionEventArgs args)
        {
            if (HorseDeployed || WagonDeployed)
            {
                NeedToGround = true;
            }
        }



        void NameHorse()
        {
                DaggerfallInputMessageBox mb = new DaggerfallInputMessageBox(DaggerfallUI.UIManager);
                mb.SetTextBoxLabel("                                                                      Name your horse");
                mb.TextPanelDistanceX = 0;
                mb.TextPanelDistanceY = 0;
                mb.InputDistanceY = 10;
                mb.InputDistanceX = -60;
                mb.TextBox.Numeric = false;
                mb.TextBox.MaxCharacters = 25;
                mb.TextBox.Text = "";
                mb.Show();
                //when input is given, it passes the input into the below method for further use.
                mb.OnGotUserInput += HorseName_OnGotUserInput;
        }

        void HorseName_OnGotUserInput(DaggerfallInputMessageBox sender, string horseNameInput)
        {
            if (horseNameInput == "" || horseNameInput == null)
            {
                HorseName = "your horse";
            }
            else
            {
                HorseName = horseNameInput;
            }
        }

        private static void AddHorseAudioSource(GameObject go)
        {
            DaggerfallAudioSource c = go.AddComponent<DaggerfallAudioSource>();
            c.AudioSource.dopplerLevel = 0;
            c.AudioSource.rolloffMode = AudioRolloffMode.Linear;
            c.AudioSource.maxDistance = 5f;
            c.AudioSource.volume = 0.7f;
            c.SetSound(SoundClips.AnimalHorse, AudioPresets.PlayRandomlyIfPlayerNear);
        }


        //Code for checking that there is room for the wagon. Commented out until I figure out how to make it work.
        static bool RoomBehind()
        {
            //GameObject player = GameManager.Instance.PlayerObject;
            //Vector3 PlayerPosition = player.transform.position;

            //RaycastHit hit;
            //Ray ray = new Ray(PlayerPosition, Vector3.back);
            //if (Physics.Raycast(ray, out hit, 5) && (hit.collider.GetComponent<MeshCollider>()))
            //{
            //    Debug.Log("RoomBehind = FALSE");
            //    return false;
            //}
            //else
            //{
            //    RaycastHit hit2;
            //    PlayerPosition = player.transform.position + (Vector3.back * 5);
            //    Ray ray2 = new Ray(PlayerPosition, Vector3.forward);
            //    if (Physics.Raycast(ray2, out hit2, 4.7f) && (hit2.collider.GetComponent<MeshCollider>()))
            //    {
            //        Debug.Log("RoomBehind = FALSE");
            //        return false;
            //    }
            //    else
            //    {
            //        Debug.Log("RoomBehind = TRUE");
            //        return true;
            //    }
            //}
            return true;
        }


        static void DestroyWagon()
        {
            if (Wagon != null)
            {
                Destroy(Wagon);
                Wagon = null;
            }
        }

        static void DestroyHorse()
        {
            if (Horse != null)
            {
                Destroy(Horse);
                Horse = null;
            }
        }
    }
}