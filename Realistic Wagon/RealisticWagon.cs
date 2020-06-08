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
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

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

        private static bool leavePrompt = true;
        private static bool mountPrompt = true;

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
                HorseMatrix = new Matrix4x4()
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
                HorseMatrix = HorseMatrix
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

            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_AdjustTransport;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_InventoryCleanup;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_AdjustTransport;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_InventoryCleanup;

            ModVersion = mod.ModInfo.ModVersion;
            mod.IsReady = true;
        }

        void Awake()
        {
            ModSettings settings = mod.GetSettings();
            leavePrompt = settings.GetBool("Settings", "leavePrompt");
            mountPrompt = settings.GetBool("Settings", "mountPrompt");
        }

        private void Update()
        {
            if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused || DaggerfallUI.Instance.FadeBehaviour.FadeInProgress)
                return;

            Debug.Log("Horse Deployed = " + HorseDeployed.ToString());

            if (transportManager.IsOnShip())
            {
                return;
            }
        
            if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && !transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart)
            {
                transportManager.TransportMode = TransportModes.Foot;
                PlaceWagon();
            }
            else if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
            {
                LeaveHorseWagonPopup();
            }
            else if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && transportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Horse)
            {
                LeaveWagonPopup();
            }
            else if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && transportManager.HasHorse() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
            {
                LeaveHorsePopup();
            }

            if (!GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside && transportManager.HasCart())
            {
                if(transportManager.HasCart())
                {
                    LeaveWagon();
                }
                if(transportManager.HasHorse())
                {
                    LeaveHorse();
                }
                transportManager.TransportMode = TransportModes.Foot;
            }

            if (transportManager.HasCart() && WagonDeployed)
            {
                NewWagonPopUp();
            }
            if (transportManager.HasHorse() && HorseDeployed)
            {
                DestroyHorse();
                HorseDeployed = false;
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
            Debug.Log("Horse Deployed = "+HorseDeployed.ToString());
            Debug.Log("Horse Position Y "+HorsePosition.y.ToString());
            Debug.Log("Player Position Y" + GameManager.Instance.PlayerGPS.transform.up.y.ToString());
        }

        private static void OnTransitionExterior_InventoryCleanup(PlayerEnterExit.TransitionEventArgs args)
        {
            Debug.Log("Cleaning up inventory");
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

        private static void LeaveHorseWagonPopup()
        {
            if (!leavePrompt)
            {
                LeaveWagon();
                LeaveHorse();
            }
            else
            {
                DaggerfallMessageBox horseWagonPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                string[] message = { "Unhitch your wagon, dismount your horse and leave them?" };
                horseWagonPopup.SetText(message);
                horseWagonPopup.OnButtonClick += HorseWagonPopup_OnButtonClick;
                horseWagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                horseWagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                horseWagonPopup.Show();
            }
        }

        private static void HorseWagonPopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                LeaveWagon();
                LeaveHorse();
            }
            else
            {
                sender.CloseWindow();
                transportManager.TransportMode = TransportModes.Cart;
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

        private static void LeaveWagonPopup()
        {
            if (!leavePrompt)
            {
                LeaveWagon();
            }
            else
            {
                DaggerfallMessageBox wagonPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                string[] message = { "Unhitch your wagon and leave it?" };
                wagonPopup.SetText(message);
                wagonPopup.OnButtonClick += WagonPopup_OnButtonClick;
                wagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                wagonPopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                wagonPopup.Show();
            }
        }

        private static void WagonPopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                LeaveWagon();
            }
            else
            {
                sender.CloseWindow();
                transportManager.TransportMode = TransportModes.Cart;
            }
        }

        private static void LeaveWagon()
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
                if(!transportManager.HasHorse())
                {
                    DaggerfallUI.MessageBox("You have no horse to pull your wagon.");
                }
                else if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.MessageBox("You are unable to levitate your wagon.");
                }
                else
                {
                    if (!mountPrompt)
                    {
                        DaggerfallUnityItem WagonItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                        DestroyWagon();
                        GameManager.Instance.PlayerEntity.Items.AddItem(WagonItem);
                        WagonDeployed = false;
                        WagonMatrix = new Matrix4x4();
                        transportManager.TransportMode = TransportModes.Cart;
                        DaggerfallUI.MessageBox("You hitch your wagon.");
                    }
                    else
                    {
                        string[] message = { "Do you wish to hitch your wagon?" };
                        wagonPopUp.SetText(message);
                        wagonPopUp.OnButtonClick += WagonPopUp_OnButtonClick;
                        wagonPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                        wagonPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                        wagonPopUp.Show();
                    }
                }
            }
            else
            {
                DaggerfallUI.MessageBox("This is not your wagon.");
            }
        }

        private static void WagonPopUp_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                sender.CloseWindow();
            }
            else
            {
                DaggerfallUnityItem WagonItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                DestroyWagon();
                GameManager.Instance.PlayerEntity.Items.AddItem(WagonItem);
                WagonDeployed = false;
                WagonMatrix = new Matrix4x4();
                sender.CloseWindow();
                transportManager.TransportMode = TransportModes.Cart;
                DaggerfallUI.MessageBox("You hitch your wagon.");
            }
        }



        //Horse code

        private static void LeaveHorsePopup()
        {
            if (!leavePrompt)
            {
                LeaveHorse();
            }
            else
            {
                DaggerfallMessageBox horsePopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                string[] message = { "Dismount and leave your horse?" };
                horsePopup.SetText(message);
                horsePopup.OnButtonClick += HorsePopup_OnButtonClick;
                horsePopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                horsePopup.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                horsePopup.Show();
            }
        }

        private static void HorsePopup_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                LeaveHorse();
            }
            else
            {
                sender.CloseWindow();
                transportManager.TransportMode = TransportModes.Horse;
            }
        }

        private static void LeaveHorse()
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

        private static void PlaceHorse(bool fromSave = false)
        {
            if (fromSave == false)
            {
                HorseMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                SetHorsePositionAndRotation();
                DaggerfallUI.MessageBox("You dismount your horse.");
            }
            else
            {
                PlaceHorseOnGround();
            }
            Horse = GameObjectHelper.CreateDaggerfallBillboardGameObject(201, 0, null);
            if (Horse == null)
            {
                Horse = GameObjectHelper.CreateDaggerfallBillboardGameObject(201, 0, null);
            }

            Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                Horse.SetActive(false);
            }
            else
            {
                Horse.SetActive(true);
            }
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
                Ray rayUp = new Ray(HorsePosition, Vector3.up);
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
                Debug.Log("Setting tent position and rotation");
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
                if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.MessageBox("You are unable to levitate your horse.");
                }
                else
                {
                    if (!mountPrompt)
                    {
                        DaggerfallUnityItem HorseItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Horse);
                        DestroyHorse();
                        GameManager.Instance.PlayerEntity.Items.AddItem(HorseItem);
                        HorseDeployed = false;
                        HorseMatrix = new Matrix4x4();
                        transportManager.TransportMode = TransportModes.Horse;
                        DaggerfallUI.MessageBox("You mount your horse.");
                    }
                    else
                    {
                        string[] message = { "Do you wish to mount your horse?" };
                        horsePopUp.SetText(message);
                        horsePopUp.OnButtonClick += HorsePopUp_OnButtonClick;
                        horsePopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                        horsePopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                        horsePopUp.Show();
                    }
                }
            }
            else
            {
                DaggerfallUI.MessageBox("This is not your horse.");
            }
        }

        private static void HorsePopUp_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                sender.CloseWindow();
            }
            else
            {
                DaggerfallUnityItem HorseItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Horse);
                DestroyHorse();
                GameManager.Instance.PlayerEntity.Items.AddItem(HorseItem);
                HorseDeployed = false;
                HorseMatrix = new Matrix4x4();
                sender.CloseWindow();
                transportManager.TransportMode = TransportModes.Horse;
                DaggerfallUI.MessageBox("You mount your horse.");
            }
        }


















        static void DestroyWagon()
        {
            if (Wagon != null)
            {
                UnityEngine.Object.Destroy(Wagon);
                Wagon = null;
            }
        }

        static void DestroyHorse()
        {
            if (Horse != null)
            {
                UnityEngine.Object.Destroy(Horse);
                Horse = null;
            }
        }
    }
}