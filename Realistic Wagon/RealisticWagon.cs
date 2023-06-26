// Project:         Realistic Wagon mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using System;
using UnityEngine;
using Wenzil.Console;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallConnect.Utility;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Utility;

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
        public DaggerfallUnityItem WagonItem;
    }


    public class RealisticWagon : MonoBehaviour, IHasModSaveData
    {
        private static string ModVersion;

        public const int templateIndex_SugarLumps = 541;
        public const int templateIndex_WagonParts = 542;
        public const int templateIndex_Horse = 548;

        private static DFPosition WagonMapPixel = null;
        private static bool WagonDeployed = false;
        private static Vector3 WagonPosition;
        private static Quaternion WagonRotation;
        private static GameObject Wagon = null;
        private static Matrix4x4 WagonMatrix;
        private static DaggerfallUnityItem WagonItem;

        private static DFPosition HorseMapPixel = null;
        private static bool HorseDeployed = false;
        private static Vector3 HorsePosition;
        private static Quaternion HorseRotation;
        private static GameObject Horse = null;
        private static Matrix4x4 HorseMatrix;
        private static string HorseName;
        private static DFPosition CurrentMapPixel;

        private static bool WagonClose;
        private static bool NeedToGroundHorse;
        private static bool NeedToGroundWagon;

        private static bool HandPaintedModels = false;

        private static int WagonDmgCounter = 0;

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
                HorseName = null,
                CurrentMapPixel = new DFPosition(),
                WagonClose = false,
                WagonItem = null

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
                WagonClose = WagonClose,
                WagonItem = WagonItem
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
            WagonItem = realisticWagonSaveData.WagonItem;

            if (WagonItem == null)
            {
                DaggerfallUnityItem newWagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                WagonItem = newWagon;
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
        

        static Mod mod;
        static RealisticWagon instance;

        public const int wagonModelID = 41241;

        static DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
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
            PlayerActivate.RegisterCustomActivation(mod, templateIndex_Horse, 0, MountHorse);

            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(templateIndex_SugarLumps, ItemGroups.UselessItems2);
            DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler(templateIndex_SugarLumps, CallHorse);

            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(templateIndex_WagonParts, ItemGroups.UselessItems2);
            DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler(templateIndex_WagonParts, RepairWagon);

            StartGameBehaviour.OnStartGame += ResetValues_OnStartGame;
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
            //PlayerGPS.OnMapPixelChanged += OnMapPixelChanged_RegisterONMR;
            EntityEffectBroker.OnNewMagicRound += OnNewMagicRound_PlaceMounts;
            PlayerEnterExit.OnTransitionInterior += OnTransitionInterior_PlaceMounts;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_PlaceMounts;
            EntityEffectBroker.OnNewMagicRound += OnNewMagicRound_WagonDamage;

            ModVersion = mod.ModInfo.ModVersion;
            mod.IsReady = true;
        }

        void Awake()
        {
            Mod hpm = ModManager.Instance.GetMod("Handpainted Models - Main");
            if (hpm != null)
                HandPaintedModels = true;
        }

        void Start()
        {
            RegisterRWCommands();
        }

        private void Update()
        {
            if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused || DaggerfallUI.Instance.FadeBehaviour.FadeInProgress)
                return;

            if (transportManager.IsOnShip())
            {
                return;
            }

            if (NeedToGroundHorse && HorseDeployed)
            {
                PlaceHorseOnGround();
                Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);
                NeedToGroundHorse = false;
            }

            if (NeedToGroundWagon && WagonDeployed)
            {
                PlaceWagonOnGround();
                Wagon.transform.SetPositionAndRotation(WagonPosition, WagonRotation);
                NeedToGroundWagon = false;
            }


            if ((HorseName == "" || HorseName == null) && transportManager.HasHorse())
            {
                NameHorse();
            }

            if (GameManager.Instance.PlayerController.isGrounded && !GameManager.Instance.IsPlayerInside)
            {
                if (playerEntity.IsInBeastForm && transportManager.HasHorse())
                {
                    transportManager.TransportMode = TransportModes.Foot;
                    LeaveHorse();
                    DaggerfallUI.MessageBox(HorseName + " bucks in fear, throwing you to the ground.");
                }
                //If you have a wagon but no horse, the wagon is dropped.
                else if (!transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart)
                {
                    transportManager.TransportMode = TransportModes.Foot;
                    PlaceWagon();
                }
                //If you have wagon and horse, but change to Foot, horse and wagon is dropped.
                else if (transportManager.HasHorse() && GameManager.Instance.TransportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
                {
                    LeaveHorseWagon();
                    DaggerfallUI.AddHUDText("You unhitch your wagon.");
                    DaggerfallUI.AddHUDText("You dismount " + HorseName + ".");
                }
                //If you have wagon and horse, but change to Horse, wagon is dropped.
                else if (transportManager.HasCart() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Horse)
                {
                    LeaveWagon();
                    DaggerfallUI.AddHUDText("You unhitch your wagon.");
                }
                //If you have horse, but change to foot, horse is dropped.
                else if (transportManager.HasHorse() && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
                {
                    LeaveHorse();
                    DaggerfallUI.AddHUDText("You dismount " + HorseName + ".");
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

            if (transportManager.HasCart() && WagonItem == null)
            {
                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                    {
                        WagonItem = item;
                    }
                }
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

        private static void ResetValues_OnStartGame(object sender, EventArgs e)
        {
            DestroyWagon();
            DestroyHorse();
            WagonMapPixel = null;
            WagonDeployed = false;
            WagonPosition = new Vector3();
            WagonRotation = new Quaternion();
            WagonMatrix = new Matrix4x4();

            HorseMapPixel = null;
            HorseDeployed = false;
            HorsePosition = new Vector3();
            HorseRotation = new Quaternion();
            HorseMatrix = new Matrix4x4();
            HorseName = "";

            CurrentMapPixel = null;
            WagonItem = null;     
        }

        private static void OnTransitionInterior_PlaceMounts(PlayerEnterExit.TransitionEventArgs args)
        {
            if (Horse != null)
                Horse.SetActive(false);
            if (Wagon != null)
                Wagon.SetActive(false);
        }

        private static void OnTransitionExterior_PlaceMounts(PlayerEnterExit.TransitionEventArgs args)
        {
            OnNewMagicRound_PlaceMounts();
        }

        private static void OnNewMagicRound_PlaceMounts()
        {
            if (HorseDeployed || WagonDeployed)
            {
                //GameObject player = GameObject.FindGameObjectWithTag("Player");
                int playerX = GameManager.Instance.PlayerGPS.CurrentMapPixel.X;
                int playerY = GameManager.Instance.PlayerGPS.CurrentMapPixel.Y;

                GameObject player = GameManager.Instance.PlayerObject;
                if (HorseDeployed && Horse != null)
                {
                    //float horseDistance = Vector3.Distance(player.transform.position, Horse.transform.position);
                    if (playerX == HorseMapPixel.X && playerY == HorseMapPixel.Y && !playerEnterExit.IsPlayerInside)
                    {
                        Horse.SetActive(true);
                        NeedToGroundHorse = true;
                    }
                    else
                    {
                        Horse.SetActive(false);
                    }
                }
                if (WagonDeployed && Wagon != null)
                {
                    //float wagonDistance = Vector3.Distance(player.transform.position, Wagon.transform.position);
                    if (playerX == WagonMapPixel.X && playerY == WagonMapPixel.Y && !playerEnterExit.IsPlayerInside)
                    {
                        Wagon.SetActive(true);
                        NeedToGroundWagon = true;
                    }
                    else
                    {
                        Wagon.SetActive(false);
                    }
                }
                //else if (HorseDeployed && HorseGrounded)
                //{
                //    GameObject player = GameManager.Instance.PlayerObject;
                //    float dist = Vector3.Distance(player.transform.position, HorsePosition);
                //    Debug.Log("[Realistic Wagon] dist to Horse is " + dist.ToString());
                //    if (dist > 1000)
                //    {
                //        Horse.SetActive(false);
                //        HorseGrounded = false;
                //    }
                //}
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
            string wagonPrompt;
            if (IsWagonClose())
                wagonPrompt = "Do you wish to abandon your old wagon and transfer items to your new wagon?";
            else
                wagonPrompt = "Do you wish to abandon your old wagon and its items?";

            string[] message = { wagonPrompt };
            
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
                if (!IsWagonClose())
                    GameManager.Instance.PlayerEntity.WagonItems.Clear();

                ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                    {
                        WagonItem = item;
                    }
                }
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
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Wagon.transform.LookAt(player.transform.position - (Vector3.up * 1.2f), Vector3.up);
                WagonRotation = Wagon.transform.rotation;
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
            MeshRenderer wagonMR = Wagon.GetComponent<MeshRenderer>();
            Material[] materials = wagonMR.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].name == "TEXTURE.088 [Index=2] (Instance)")
                {
                    Material newMaterial = DaggerfallUnity.Instance.MaterialReader.GetMaterial(431, 3);
                    materials[i] = newMaterial;
                    wagonMR.materials = materials;
                }
                if (materials[i].name == "TEXTURE.088 [Index=3] (Instance)")
                {
                    Material newMaterial = DaggerfallUnity.Instance.MaterialReader.GetMaterial(431, 0);
                    materials[i] = newMaterial;
                    wagonMR.materials = materials;
                }
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
            Ray ray;
            if (!HandPaintedModels)
                ray = new Ray(WagonPosition, Vector3.down);
            else
                ray = new Ray(WagonPosition + (Vector3.down * 0.5f), Vector3.down);
            if (Physics.Raycast(ray, out hit, 1000))
            {
                if (!HandPaintedModels)
                {
                    WagonPosition = hit.point + hit.transform.up;
                }
                else
                    WagonPosition = hit.point + (hit.transform.up * 0.8f);           
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 newWagonPos = WagonPosition;
                newWagonPos.y = player.transform.position.y;
                Ray rayFromPlayer = new Ray(newWagonPos + Vector3.up, Vector3.down);
                if (Physics.Raycast(rayFromPlayer, out hit, 1000))
                {
                    if (!HandPaintedModels)
                    {
                        WagonPosition = hit.point + hit.transform.up;
                    }
                    else
                        WagonPosition = hit.point + (hit.transform.up * 0.8f);
                }
                else
                {
                    Ray rayUp = new Ray(newWagonPos + (Vector3.up * 500f), Vector3.down);
                    if (Physics.Raycast(rayUp, out hit, 1000))
                    {
                        if (!HandPaintedModels)
                        {
                            WagonPosition = hit.point + hit.transform.up;
                        }
                        else
                            WagonPosition = hit.point + (hit.transform.up * 0.8f);
                    }
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
            Ray ray;
            if (!HandPaintedModels)
                ray = new Ray(WagonPosition, Vector3.down);
            else
                ray = new Ray(WagonPosition + (Vector3.down * 0.5f), Vector3.down);
            if (Physics.Raycast(ray, out hit, 10))
            {
                if (!HandPaintedModels)
                {
                    WagonPosition = hit.point + hit.transform.up;
                }
                else
                    WagonPosition = hit.point + (hit.transform.up * 0.8f);

                if (WagonRotation == null)
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
                    //DaggerfallUI.MessageBox("You have no horse to pull your wagon.");
                    DaggerfallUI.Instance.InventoryWindow.AllowDungeonWagonAccess();
                    DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
                }
                else if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.AddHUDText("You are unable to levitate your wagon");
                }
                else
                {
                    DaggerfallUnityItem newWagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                    if (WagonItem == null)
                    {
                        WagonItem = newWagon;
                    }
                    newWagon.currentCondition = WagonItem.currentCondition;
                    if (WagonItem.currentCondition >= 1)
                    {
                        DestroyWagon();
                        GameManager.Instance.PlayerEntity.Items.AddItem(newWagon);
                        WagonDeployed = false;
                        WagonMatrix = new Matrix4x4();
                        transportManager.TransportMode = TransportModes.Cart;
                        DaggerfallUI.AddHUDText("You hitch your wagon");                   
                    }
                    else
                        DaggerfallUI.AddHUDText("Your wagon is broken");
                }
            }
            else
            {
                DaggerfallUI.AddHUDText("This is not your wagon");
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
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Horse = MeshReplacement.ImportCustomFlatGameobject(templateIndex_Horse, 0, HorsePosition, null);
            if (Horse == null)
                Horse = GameObjectHelper.CreateDaggerfallBillboardGameObject(templateIndex_Horse, 0, null);
            if (fromSave == false)
            {
                HorseMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                SetHorsePositionAndRotation();
                Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);
                //Horse.transform.LookAt(player.transform.position - (Vector3.up * 1.3f), Vector3.up);
                //Horse.transform.LookAt(player.transform.position - Vector3.up, Vector3.up);
                Vector3 horseTarget = new Vector3(player.transform.position.x, Horse.transform.position.y, player.transform.position.z);
                Horse.transform.LookAt(horseTarget, Vector3.up);
                HorseRotation = Horse.transform.rotation;
            }
            else
                Horse.transform.SetPositionAndRotation(HorsePosition, HorseRotation);

            if (Horse.GetComponent<BoxCollider>() == null)
                AddTrigger(Horse);
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
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            RaycastHit hit;
            bool modelReplacement = false;
            if (Horse != null && Horse.GetComponent<DaggerfallBillboard>() == null)
                modelReplacement = true;
            Ray rayDown = new Ray(HorsePosition, Vector3.down);
            if (Physics.Raycast(rayDown, out hit, 10000))
            {
                Debug.Log("[Realistic Wagon] hit = " + hit.ToString());
                if (modelReplacement)
                    HorsePosition = hit.point;
                else
                    HorsePosition = hit.point + (Vector3.up * 1.1f);
            }
            else
            {
                Vector3 newHorsePos = HorsePosition;
                newHorsePos.y = player.transform.position.y;
                Ray rayFromPlayer = new Ray(newHorsePos + (Vector3.up * 0.5f), Vector3.down);
                if (Physics.Raycast(rayFromPlayer, out hit, 1000))
                {
                    Debug.Log("[Realistic Wagon] hit = " + hit.ToString());
                    if (modelReplacement)
                        HorsePosition = hit.point;
                    else
                        HorsePosition = hit.point + (hit.transform.up * 1.1f);
                }
                else
                {
                    Ray rayUp = new Ray(newHorsePos + (Vector3.up * 500f), Vector3.down);
                    if (Physics.Raycast(rayUp, out hit, 1000))
                    {
                        Debug.Log("[Realistic Wagon] hit = " + hit.ToString());
                        if (modelReplacement)
                            HorsePosition = hit.point;
                        else
                            HorsePosition = hit.point + (hit.transform.up * 1.1f);
                    }
                }
            }
        }

        private static void SetHorsePositionAndRotation()
        {
            GameObject player = GameManager.Instance.PlayerObject;
            HorsePosition = player.transform.position - (player.transform.forward * 1.5f);
            HorseMatrix = player.transform.localToWorldMatrix;
            bool modelReplacement = false;
            if (Horse != null && Horse.GetComponent<DaggerfallBillboard>() == null)
                modelReplacement = true;
            RaycastHit hit;
            Ray ray = new Ray(HorsePosition, Vector3.down);
            if (Physics.Raycast(ray, out hit, 10))
            {
                if (modelReplacement)
                    HorsePosition = hit.point;
                else
                    HorsePosition = hit.point + (hit.transform.up * 1.1f);
                HorseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                Debug.Log("[Realistic Wagon] horserotation set by SetHorsePositionAndRotation()");
            }
            else
            {
                Debug.Log("Setting horse position and rotation failed");
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
                else if (playerEntity.IsInBeastForm)
                {
                    DaggerfallUI.MessageBox(HorseName + " shies away from you.");
                }
                else if (!GameManager.Instance.PlayerController.isGrounded)
                {
                    DaggerfallUI.AddHUDText(HorseName+" is unable to levitate.");
                }
                else
                {
                    DaggerfallUnityItem HorseItem = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Horse);
                    DestroyHorse();
                    GameManager.Instance.PlayerEntity.Items.AddItem(HorseItem);
                    HorseDeployed = false;
                    HorseMatrix = new Matrix4x4();
                    transportManager.TransportMode = TransportModes.Horse;
                    DaggerfallUI.AddHUDText("You mount " + HorseName + ".");
                    if (WagonDeployed)
                    {
                        GameObject player = GameManager.Instance.PlayerObject;
                        float dist = Vector3.Distance(player.transform.position, WagonPosition);
                        Debug.Log("Dsitance to wagon = " + dist.ToString());

                        if (dist < 5 && WagonItem.currentCondition >= 1)
                        {
                            DaggerfallUnityItem newWagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                            if (WagonItem == null)
                            {
                                WagonItem = newWagon;
                            }
                            newWagon.currentCondition = WagonItem.currentCondition;
                            DestroyWagon();
                            GameManager.Instance.PlayerEntity.Items.AddItem(newWagon);
                            WagonDeployed = false;
                            WagonMatrix = new Matrix4x4();
                            transportManager.TransportMode = TransportModes.Cart;
                            DaggerfallUI.AddHUDText("You hitch your wagon.");
                        }
                    }
                }
            }
            else
            {
                DaggerfallUI.AddHUDText("This is not " + HorseName + ".");
            }
        }

        private static void OnTransitionExterior_HeightExitCorrection(PlayerEnterExit.TransitionEventArgs args)
        {
            if (HorseDeployed)
            {
                NeedToGroundHorse = true;
            }
            if (WagonDeployed)
            {
                NeedToGroundWagon = true;
            }
        }



        void NameHorse()
        {
                DaggerfallInputMessageBox mb = new DaggerfallInputMessageBox(DaggerfallUI.UIManager);
                mb.SetTextBoxLabel("                                                 Name your horse");
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


        static bool CallHorse(DaggerfallUnityItem item, ItemCollection collection)
        {
            
            if (!HorseDeployed)
                DaggerfallUI.MessageBox("You have no horse to call.");
            else if (!playerEnterExit.IsPlayerInside && GameManager.Instance.PlayerController.isGrounded && IsHorseClose())
            {
                item.weightInKg -= 0.1f;
                if (item.weightInKg <= 0)
                    playerEntity.Items.RemoveItem(item);
                DestroyHorse();
                PlaceHorse();
                DaggerfallUI.MessageBox(HorseName + " trots up to you.");
            }
            else
                DaggerfallUI.MessageBox(HorseName + " is too far away.");
            return false;
        }

        static bool RepairWagon(DaggerfallUnityItem parts, ItemCollection collection)
        {
            bool hasCart = GameManager.Instance.TransportManager.HasCart();
            if (!hasCart)
            {
                if (WagonDeployed)
                {
                    GameObject player = GameManager.Instance.PlayerObject;
                    float dist = Vector3.Distance(player.transform.position, WagonPosition);
                    if (dist < 5)
                    {
                        if (WagonItem == null)
                        {
                            DaggerfallUnityItem newWagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                            WagonItem = newWagon;
                        }
                        int wagonDmg = WagonItem.maxCondition - WagonItem.currentCondition;
                        if (wagonDmg == 0)
                        {
                            DaggerfallUI.MessageBox("Your wagon is not in need of repair.");
                        }
                        else
                        {
                            if (parts.currentCondition >= wagonDmg)
                            {
                                WagonItem.currentCondition = WagonItem.maxCondition;
                                parts.currentCondition -= wagonDmg;
                            }
                            else if (parts.currentCondition < wagonDmg)
                            {
                                WagonItem.currentCondition += parts.currentCondition;
                                GameManager.Instance.PlayerEntity.Items.RemoveItem(parts);
                                GameManager.Instance.PlayerEntity.WagonItems.RemoveItem(parts);
                            }
                            DaggerfallDateTime timeNow = DaggerfallUnity.Instance.WorldTime.Now;
                            timeNow.RaiseTime(wagonDmg * 2);
                            GameManager.Instance.PlayerEntity.CurrentFatigue /= 2;
                            DaggerfallUI.MessageBox("You repair the wagon.");
                        }
                        
                    }
                    else
                        DaggerfallUI.MessageBox("Your wagon is too far away.");
                }
                else
                    DaggerfallUI.MessageBox("You do not own a wagon to repair.");
            }
            else if (hasCart)
            {
                if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                {
                    ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                    for (int i = 0; i < playerItems.Count; i++)
                    {
                        DaggerfallUnityItem wagon = playerItems.GetItem(i);
                        if (wagon != null && wagon.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                        {
                            int wagonDmg = wagon.maxCondition - wagon.currentCondition;
                            if (wagonDmg == 0)
                            {
                                DaggerfallUI.MessageBox("Your wagon is not in need of repair.");
                            }
                            else
                            {
                                if (parts.currentCondition >= wagonDmg)
                                {
                                    wagon.currentCondition = wagon.maxCondition;
                                    parts.currentCondition -= wagonDmg;
                                }
                                else if (parts.currentCondition < wagonDmg)
                                {
                                    wagon.currentCondition += parts.currentCondition;
                                    GameManager.Instance.PlayerEntity.Items.RemoveItem(parts);
                                    GameManager.Instance.PlayerEntity.WagonItems.RemoveItem(parts);
                                }
                                WagonItem = wagon;
                                DaggerfallUI.MessageBox("You repair the wagon.");
                            }
                        }
                    }
                }
                else
                    DaggerfallUI.MessageBox("Your wagon is too far away.");
               
            }

            return false;
        }

        private static void OnNewMagicRound_WagonDamage()
        {
            WagonDmgCounter++;
            if (WagonDmgCounter >= 5)
            {
                WagonDmgCounter = 0;
                bool ingame = !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress && !GameManager.Instance.IsPlayerOnHUD;
                if (ingame && GameManager.Instance.TransportManager.HasCart() && !GameManager.Instance.PlayerMotor.IsStandingStill && !playerEnterExit.IsPlayerInside)
                {
                    int wagonDamage = WagonDamage();
                    string dmgText = "";
                    if (wagonDamage >= 1)
                    {
                        ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
                        for (int i = 0; i < playerItems.Count; i++)
                        {
                            DaggerfallUnityItem item = playerItems.GetItem(i);
                            if (item != null && item.IsOfTemplate(ItemGroups.Transportation, (int)Transportation.Small_cart))
                            {
                                item.currentCondition -= wagonDamage;
                                WagonItem = item;
                                if (item.currentCondition <= 0)
                                    playerItems.RemoveItem(item);
                            }
                        }

                        if (WagonItem == null)
                        {
                            DaggerfallUnityItem newWagon = ItemBuilder.CreateItem(ItemGroups.Transportation, (int)Transportation.Small_cart);
                            WagonItem = newWagon;
                        }

                        if (WagonItem.currentCondition <= 0)
                        {
                            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
                            DaggerfallUI.MessageBox("With a loud wooden crack, the wagon breaks down.");
                            PlaceWagon();
                            transportManager.TransportMode = TransportModes.Horse;
                        }
                        else if (WagonItem.currentCondition <= (WagonItem.maxCondition / 6))
                        {
                            dmgText = "The wagon makes a loud creaking sound...";
                        }
                        else if (WagonItem.currentCondition <= (WagonItem.maxCondition / 3))
                        {
                            dmgText = "The wagon squeaks and creaks...";
                        }

                        if (dmgText != "" && Dice100.SuccessRoll(30))
                        {
                            ModManager.Instance.SendModMessage("TravelOptions", "isTravelActive", null, (string message, object data) =>
                            {
                                if ((bool)data)
                                    ModManager.Instance.SendModMessage("TravelOptions", "showMessage", dmgText);
                                else
                                    DaggerfallUI.AddHUDText(dmgText);
                            });
                        }
                    }
                }
            }
        }

        private static int WagonDamage()
        {
            int dmg = 0;
            bool roadFollow = true;
            bool pathFollow = true;
            ModManager.Instance.SendModMessage("TravelOptions", "isFollowingRoad", null, (string message, object data) =>
            {
                roadFollow = (bool)data;
            });

            ModManager.Instance.SendModMessage("TravelOptions", "isPathFollowing", null, (string message, object data) =>
            {
                pathFollow = (bool)data;
            });


            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            if (!playerGPS.IsPlayerInLocationRect)
            {
                switch (playerGPS.CurrentClimateIndex)
                {
                    case (int)MapsFile.Climates.Desert2:
                    case (int)MapsFile.Climates.Desert:
                    case (int)MapsFile.Climates.Subtropical:
                        dmg += 1;
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Swamp:
                        dmg += 3;
                        break;
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                        dmg += 2;
                        break;
                    case (int)MapsFile.Climates.MountainWoods:
                    case (int)MapsFile.Climates.Mountain:
                        dmg += 5;
                        break;
                }
            }
            if (!pathFollow && !roadFollow)
                dmg *= 2;
            else if (pathFollow && !roadFollow)
                dmg += 1;

            if (GameManager.Instance.PlayerEntity.WagonWeight > ItemHelper.WagonKgLimit / 3)
                dmg += 1;

            return dmg;
        }

        private static bool IsHorseClose()
        {
            int playerX = GameManager.Instance.PlayerGPS.CurrentMapPixel.X;
            int playerY = GameManager.Instance.PlayerGPS.CurrentMapPixel.Y;
            return Math.Abs(playerX - HorseMapPixel.X) <= 1 && Math.Abs(playerY - HorseMapPixel.Y) <= 1;
        }

        private static bool IsWagonClose()
        {
            int playerX = GameManager.Instance.PlayerGPS.CurrentMapPixel.X;
            int playerY = GameManager.Instance.PlayerGPS.CurrentMapPixel.Y;
            return playerX == WagonMapPixel.X && playerY == WagonMapPixel.Y;
        }

        private static void AddTrigger(GameObject obj)
        {
            Debug.Log("[Realistic Wagon] AddTrigger() " + obj.name);
            BoxCollider boxTrigger = obj.AddComponent<BoxCollider>();
            {
                boxTrigger.isTrigger = true;
                boxTrigger.center = new Vector3(0f, 1f, -0.5f);
                boxTrigger.size = new Vector3(0.5f, 2f, 2.3f);
            }
        }

        public static void RegisterRWCommands()
        {
            Debug.Log("[Realistic Wagon] Trying to register console commands.");
            try
            {
                ConsoleCommandsDatabase.RegisterCommand(GetWagon.name, GetWagon.description, GetWagon.usage, GetWagon.Execute);
                ConsoleCommandsDatabase.RegisterCommand(GetHorse.name, GetHorse.description, GetHorse.usage, GetHorse.Execute);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Error Registering RealisticWagon Console commands: {0}", e.Message));
            }
        }

        private static class GetWagon
        {
            public static readonly string name = "wagon_rescue";
            public static readonly string description = "Place wagon behind player.";
            public static readonly string usage = "wagon_rescue";

            public static string Execute(params string[] args)
            {
                string result = "error";
                if (!WagonDeployed)
                    result = "No wagon to rescue";
                else if (!playerEnterExit.IsPlayerInside && GameManager.Instance.PlayerController.isGrounded)
                {
                    DestroyWagon();
                    PlaceWagon();
                    result = "Wagon Rescued";
                }
                else
                    result = "Command only possible while on the ground outside.";
                return result;
            }
        }

        private static class GetHorse
        {
            public static readonly string name = "horse_rescue";
            public static readonly string description = "Place horse behind player.";
            public static readonly string usage = "horse_rescue";

            public static string Execute(params string[] args)
            {
                string result = "error";
                if (!HorseDeployed)
                    result = "No horse to rescue";
                else if (!playerEnterExit.IsPlayerInside && GameManager.Instance.PlayerController.isGrounded)
                {
                    DestroyHorse();
                    PlaceHorse();
                    result = "Horse Rescued";
                }
                else
                    result = "Command only possible while on the ground outside.";
                return result;
            }
        }
    }
}