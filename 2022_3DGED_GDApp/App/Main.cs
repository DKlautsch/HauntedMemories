#region Pre-compiler directives

#define DEMO
#define SHOW_DEBUG_INFO

#endregion

using GD.Core;
using GD.Engine;
using GD.Engine.Events;
using GD.Engine.Globals;
using GD.Engine.Inputs;
using GD.Engine.Managers;
using GD.Engine.Parameters;
using GD.Engine.Utilities;
using JigLibX.Collision;
using JigLibX.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Application = GD.Engine.Globals.Application;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Cue = GD.Engine.Managers.Cue;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace GD.App
{
    public class Main : Game
    {
        #region Fields

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private BasicEffect unlitEffect;
        private BasicEffect litEffect;

        public CameraManager cameraManager;
        private SceneManager<Scene> sceneManager;
        private SoundManager soundManager;
        private PhysicsManager physicsManager;
        private RenderManager renderManager;
        private EventDispatcher eventDispatcher;
        private GameObject playerGameObject;
        private PickingManager pickingManager;
        private StateManager stateManager;
        private GameObject uiTextureGameObject;

        private UserInterfaceManager hudManager;
        private Render2DManager hudRenderManager;
        private UserInterfaceManager menuManager;
        private Render2DManager menuRenderManager;

#if DEMO

        private event EventHandler OnChanged;

#endif

        #endregion Fields

        #region Constructors

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #endregion Constructors

        #region Actions - Initialize

#if DEMO

        private void DemoCode()
        {
            //shows how we can create an event, register for it, and raise it in Main::Update() on Keys.E press
            DemoEvent();

            //shows us how to listen to a specific event
            DemoStateManagerEvent();

            Demo3DSoundTree();
        }

        private void Demo3DSoundTree()
        {
            //var camera = Application.CameraManager.ActiveCamera.AudioListener;
            //var audioEmitter = //get tree, get emitterbehaviour, get audio emitter

            //object[] parameters = {"sound name", audioListener, audioEmitter};

            //EventDispatcher.Raise(new EventData(EventCategoryType.Sound,
            //    EventActionType.OnPlay3D, parameters));

            //throw new NotImplementedException();
        }

        private void DemoStateManagerEvent()
        {
            EventDispatcher.Subscribe(EventCategoryType.Player, HandleEvent);
        }

        private void HandleEvent(EventData eventData)
        {
            switch (eventData.EventActionType)
            {
                case EventActionType.OnWin:
                    System.Diagnostics.Debug.WriteLine(eventData.Parameters[0] as string);
                    break;

                case EventActionType.OnLose:
                    System.Diagnostics.Debug.WriteLine(eventData.Parameters[2] as string);
                    break;

                default:
                    break;
            }
        }

        private void DemoEvent()
        {
            OnChanged += HandleOnChanged;
        }

        private void HandleOnChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{e} was sent by {sender}");
        }

#endif

        protected override void Initialize()
        {
            //moved spritebatch initialization here because we need it in InitializeDebug() below
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //core engine - common across any game
            InitializeEngine(AppData.APP_RESOLUTION, true, true);

            //game specific content
            InitializeLevel("My Amazing Game", AppData.SKYBOX_WORLD_SCALE);

#if SHOW_DEBUG_INFO
            InitializeDebug();
#endif

#if DEMO
            DemoCode();
#endif

            base.Initialize();
        }

        #endregion Actions - Initialize

        #region Actions - Level Specific

        protected override void LoadContent()
        {
            //moved spritebatch initialization to Main::Initialize() because we need it in InitializeDebug()
            //_spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private void InitializeLevel(string title, float worldScale)
        {
            //set game title
            SetTitle(title);

            //load sounds, textures, models etc
            LoadMediaAssets();

            //initialize curves used by cameras
            InitializeCurves();

            //initialize rails used by cameras
            InitializeRails();

            //add scene manager and starting scenes
            InitializeScenes();

            //add collidable drawn stuff
            InitializeCollidableContent(worldScale);

            //add non-collidable drawn stuff
            InitializeNonCollidableContent(worldScale);

            //add the player
            //InitializePlayer();

#if DEMO
            //Raise all the events that I want to happen at the start
            //object[] parameters = { "epic_soundcue" };
            //EventDispatcher.Raise(
            //    new EventData(EventCategoryType.Player,
            //    EventActionType.OnSpawnObject,
            //    parameters));

#endif

            InitializeHUD();
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            var mainMenuScene = new Scene2D("main menu");

            GameObject uiTextureGameObject = null;

            #region Add Background

            uiTextureGameObject = new GameObject("background");
            var texture = Content.Load<Texture2D>("Assets/Textures/Menu/Backgrounds/MainMenuConcept");
            var material = new SpriteMaterial(texture, Color.White);

            var scaleToWindow = new Vector3(((float)_graphics.PreferredBackBufferWidth) / texture.Width,
                ((float)_graphics.PreferredBackBufferHeight) / texture.Height, 1);

            uiTextureGameObject.Transform = new Transform(
                scaleToWindow, //s
                new Vector3(0, 0, 0), //r
                new Vector3(0, 0, 0)); //t

            var uiElement = new UITextureElement();
            uiTextureGameObject.AddComponent(new SpriteRenderer(material, uiElement));

            //add to scene2D
            mainMenuScene.Add(uiTextureGameObject);

            #endregion

            #region Add Scene to Manager and Set Active

            //add scene2D to menu manager
            menuManager.Add(mainMenuScene.ID, mainMenuScene);

            //what menu do i see first?
            menuManager.SetActiveScene(mainMenuScene.ID);

            #endregion
        }

        private void InitializeHUD()
        {
            var mainHUD = new Scene2D("game HUD");

            #region Add HUD Element

            uiTextureGameObject = new GameObject("a square");
            uiTextureGameObject.Transform = new Transform(
                new Vector3(1, 1, 0), //s
                new Vector3(0, 0, 45), //r
                new Vector3(50, 50, 0)); //t

            var material = new SpriteMaterial(Content.Load<Texture2D>("Assets/Textures/DemoTextures/UI/white32x32"), Color.Red);
            var uiElement = new UITextureElement();
            uiTextureGameObject.AddComponent(
                new SpriteRenderer(material, uiElement));

            //add to scene2D
            mainHUD.Add(uiTextureGameObject);

            #endregion

            #region Add Scene to Manager and Set Active

            //add scene2D to manager
            hudManager.Add(mainHUD.ID, mainHUD);

            //what ui do i see first?
            hudManager.SetActiveScene(mainHUD.ID);

            #endregion
        }

        private void SetTitle(string title)
        {
            Window.Title = title.Trim();
        }

        private void LoadMediaAssets()
        {
            //sounds, models, textures
            LoadSounds();
            LoadTextures();
            LoadModels();
        }

        private void LoadSounds()
        {
            //var soundEffect =
            //    Content.Load<SoundEffect>("Assets/Audio/Diegetic/explode1");

            ////add the new sound effect
            //soundManager.Add(new Cue(
            //    "boom1",
            //    soundEffect,
            //    SoundCategoryType.Alarm,
            //    new Vector3(1, 1, 0),
            //    false));

            var soundEffect =
                Content.Load<SoundEffect>("Assets/Audio/Diegetic/GrassFootsteps-2");

            //add the new sound effect
            soundManager.Add(new Cue(
                "boom1",
                soundEffect,
                SoundCategoryType.Alarm,
                new Vector3(1, 1, 0),
                true));
            soundManager.Play2D("boom1");

            soundEffect =
             Content.Load<SoundEffect>("Assets/Audio/Diegetic/Better-Tile-Walk-4");

            //add the new sound effect
            soundManager.Add(new Cue(
                "boom2",
                soundEffect,
                SoundCategoryType.Alarm,
                new Vector3(0.3f, 0.5f, 0),
                true));
            soundManager.Play2D("boom2");
            Application.SoundManager.Pause("boom2");

            soundEffect =
               Content.Load<SoundEffect>("Assets/Audio/Diegetic/Wood_Running_1.1_-2-2");

            //add the new sound effect
            soundManager.Add(new Cue(
                "boom3",
                soundEffect,
                SoundCategoryType.Alarm,
                new Vector3(0.25f, 0.5f, 0),
                true));
            soundManager.Play2D("boom3");
            Application.SoundManager.Pause("boom3");
        }

        private void LoadTextures()
        {
            //load and add to dictionary
            //Content.Load<Texture>
        }

        private void LoadModels()
        {
            //load and add to dictionary
        }

        private void InitializeCurves()
        {
            //load and add to dictionary
        }

        private void InitializeRails()
        {
            //load and add to dictionary
        }

        private void InitializeScenes()
        {
            //initialize a scene
            var scene = new Scene("labyrinth");

            //add scene to the scene manager
            sceneManager.Add(scene.ID, scene);

            //don't forget to set active scene
            sceneManager.SetActiveScene("labyrinth");
        }

        private void InitializeEffects()
        {
            //only for skybox with lighting disabled
            unlitEffect = new BasicEffect(_graphics.GraphicsDevice);
            unlitEffect.TextureEnabled = true;

            //all other drawn objects
            litEffect = new BasicEffect(_graphics.GraphicsDevice);
            litEffect.TextureEnabled = true;
            litEffect.LightingEnabled = true;
            litEffect.EnableDefaultLighting();
            litEffect.SpecularPower = 200;
            litEffect.AmbientLightColor = Color.DarkBlue.ToVector3();

            litEffect.DirectionalLight0.SpecularColor = Color.Black.ToVector3();
            litEffect.DirectionalLight2.SpecularColor = Color.Black.ToVector3();
            litEffect.DirectionalLight2.Direction = new Vector3(0.75f, 0.1f, 0);
            litEffect.DirectionalLight2.DiffuseColor = Color.DarkOrange.ToVector3();
        }

        private void InitializeCameras()
        {
            //camera
            GameObject cameraGameObject = null;

            #region Third Person

            cameraGameObject = new GameObject(AppData.THIRD_PERSON_CAMERA_NAME);
            cameraGameObject.Transform = new Transform(null, null, null);
            cameraGameObject.AddComponent(new Camera(
                AppData.FIRST_PERSON_HALF_FOV, //MathHelper.PiOver2 / 2,
                (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight,
                AppData.FIRST_PERSON_CAMERA_NCP, //0.1f,
                AppData.FIRST_PERSON_CAMERA_FCP,
                new Viewport(0, 0, _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight))); // 3000

            cameraGameObject.AddComponent(new ThirdPersonController());

            cameraManager.Add(cameraGameObject.Name, cameraGameObject);

            #endregion

            #region First Person

            //camera 1
            cameraGameObject = new GameObject(AppData.FIRST_PERSON_CAMERA_NAME);
            cameraGameObject.Transform = new Transform(null, null,
                AppData.FIRST_PERSON_DEFAULT_CAMERA_POSITION);

            #region Camera - View & Projection

            cameraGameObject.AddComponent(
             new Camera(
             AppData.FIRST_PERSON_HALF_FOV, //MathHelper.PiOver2 / 2,
             (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight,
             AppData.FIRST_PERSON_CAMERA_NCP, //0.1f,
             AppData.FIRST_PERSON_CAMERA_FCP,
             new Viewport(0, 0, _graphics.PreferredBackBufferWidth,
             _graphics.PreferredBackBufferHeight))); // 3000

            #endregion

            #region Collision - Add capsule

            //adding a collidable surface that enables acceleration, jumping
            var characterCollider = new CharacterCollider(cameraGameObject, true);

            cameraGameObject.AddComponent(characterCollider);
            characterCollider.AddPrimitive(new Capsule(
                cameraGameObject.Transform.Translation,
                Matrix.CreateRotationX(MathHelper.PiOver2),
                1, 3.6f),
                new MaterialProperties(0.2f, 0.8f, 0.7f));
            characterCollider.Enable(cameraGameObject, false, 1);

            #endregion

            #region Collision - Add Controller for movement (now with collision)

            cameraGameObject.AddComponent(new CollidableFirstPersonController(cameraGameObject,
                characterCollider,
                AppData.FIRST_PERSON_MOVE_SPEED, AppData.FIRST_PERSON_STRAFE_SPEED,
                AppData.PLAYER_ROTATE_SPEED_VECTOR2, AppData.FIRST_PERSON_CAMERA_SMOOTH_FACTOR, true,
                AppData.PLAYER_COLLIDABLE_JUMP_HEIGHT));

            #endregion

            #region 3D Sound

            //added ability for camera to listen to 3D sounds
            cameraGameObject.AddComponent(new AudioListenerBehaviour());

            #endregion

            cameraManager.Add(cameraGameObject.Name, cameraGameObject);

            #endregion First Person

            #region Security

            //camera 2
            cameraGameObject = new GameObject(AppData.SECURITY_CAMERA_NAME);

            cameraGameObject.Transform
                = new Transform(null,
                null,
                new Vector3(0, 2, 25));

            //add camera (view, projection)
            cameraGameObject.AddComponent(new Camera(
                MathHelper.PiOver2 / 2,
                (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight,
                0.1f, 3500,
                new Viewport(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)));

            //add rotation
            cameraGameObject.AddComponent(new CycledRotationBehaviour(
                AppData.SECURITY_CAMERA_ROTATION_AXIS,
                AppData.SECURITY_CAMERA_MAX_ANGLE,
                AppData.SECURITY_CAMERA_ANGULAR_SPEED_MUL,
                TurnDirectionType.Right));

            //adds FOV change on mouse scroll
            cameraGameObject.AddComponent(new CameraFOVController(AppData.CAMERA_FOV_INCREMENT_LOW));

            cameraManager.Add(cameraGameObject.Name, cameraGameObject);

            #endregion Security

            #region Curve

            Curve3D curve3D = new Curve3D(CurveLoopType.Oscillate);
            curve3D.Add(new Vector3(0, 2, 5), 0);
            curve3D.Add(new Vector3(0, 5, 10), 1000);
            curve3D.Add(new Vector3(0, 8, 25), 2500);
            curve3D.Add(new Vector3(0, 5, 35), 4000);

            cameraGameObject = new GameObject(AppData.CURVE_CAMERA_NAME);
            cameraGameObject.Transform =
                new Transform(null, null, null);
            cameraGameObject.AddComponent(new Camera(
                MathHelper.PiOver2 / 2,
                (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight,
                0.1f, 3500,
                  new Viewport(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)));

            //define what action the curve will apply to the target game object
            var curveAction = (Curve3D curve, GameObject target, GameTime gameTime) =>
            {
                target.Transform.SetTranslation(curve.Evaluate(gameTime.TotalGameTime.TotalMilliseconds, 4));
            };

            cameraGameObject.AddComponent(new CurveBehaviour(curve3D, curveAction));

            cameraManager.Add(cameraGameObject.Name, cameraGameObject);

            #endregion Curve

            cameraManager.SetActiveCamera(AppData.FIRST_PERSON_CAMERA_NAME);
        }

        private void InitializeCollidableContent(float worldScale)
        {            
            InitializeCollidableGround(worldScale);
            
            //DEMO MODELS
            InitializeCollidableBox();
            InitializeCollidableHighDetailMonkey();

            //OUR MODELS
            InitializeWalls();
            InitializeTowerModels();
        }

        private void InitializeNonCollidableContent(float worldScale)
        {
            InitializeXYZ();

            //create sky
            InitializeSkyBox(worldScale);

            //quad with crate texture
            InitializeDemoQuad();

            //load an FBX and draw
            InitializeDemoModel();

            //TODO - remove these test methods later
            //test for one team
            InitializeRadarModel();
            //test for another team
            InitializeDemoButton();

            //quad with a tree texture
            InitializeTreeQuad();


            /* OUR MODELS */

            //Big Stuctures
            InitializeDoors();
            InitializeFloors();
            InitializeStairs();
            InitializeEntrance();            

            /* Filler Models */

            //Kitchen               
            InitializeMug();
            InitializeMilk();
            InitializeStove();
            InitializeBoxes();
            InitializeSpoon();
            InitializeTable();
            InitializeWheetBags();
            InitializeKitchenPot();
            InitializeRollingPin();
            InitializeKitchenVase();
            InitializeKitchenBoard();
            InitializeKitchenKnife();
            InitializeKitchenPlate();
            InitializeKitchenLight();
            InitializeKitchenChairs();

            //Garden
            InitializeWC();
            InitializeKey();
            InitializeRing();
            InitializeWell();
            InitializeRock();
            InitializeTrees();
            InitializeBench();
            InitializeBucket();
            InitializeShrubs();
            InitializeTorches();
            InitializeCarrige();
            InitializeGrassModels();
            InitializeCombatDummy();
            InitializeGardenBarrels();

            //Main Hall
            InitializeCandle();
            InitializeHallSofa();
            InitializeHallTable();

            /* Important Quest Models */

            InitializeToolbox();
            InitializeClipboard();

            //Character
            //InitializeLadyRoesia(); 
        }

        #region OUR MODELS

        private void InitializeRing()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/celtic_ring_wire_229154215_BaseColor");
            var model = Content.Load<Model>("Assets/Models/Garden/celtic_ring");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var ring = new GameObject("CelticRing",
                ObjectType.Static, RenderType.Opaque);
            ring.Transform = new Transform(0.03f * Vector3.One,
                Vector3.Zero, new Vector3(30, 1, -28.4f));
            ring.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(ring);
        }
        //Placed & Textured
        private void InitializeCandle()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/Candle_low_Candle01_BaseColor");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Candle_low");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var candle = new GameObject("CandleKitchenTable",
                ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 2; i++)
            {
                candle = new GameObject("CandleKitchenTable0" + i,
                ObjectType.Static, RenderType.Opaque);
                candle.Transform = new Transform(0.03f * Vector3.One,
                    Vector3.Zero, new Vector3(-37.9f - (0.6f * i), 1.74f, -27.5f - (10.3f * i)));
                candle.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(candle);
            }
        }
        //Placed & Textured
        private void InitializeHallTable()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Hall/wood_table_001_diff_1k");
            var model = Content.Load<Model>("Assets/Models/Hall/Table");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var table = new GameObject("HallTable",
                ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 2; i++)
            {
                table = new GameObject("HallTable0" + i,
                     ObjectType.Static, RenderType.Opaque);
                table.Transform = new Transform(0.3f * Vector3.One,
                 Vector3.Zero, new Vector3(-38 - (1 * i), 0, -27 - (10.8f * i)));
                table.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(table);
            }
        }
        //Placed & Textured
        private void InitializeHallSofa()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Hall/initialShadingGroup_albedo");
            var model = Content.Load<Model>("Assets/Models/Hall/LoungeSofa");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var sofa = new GameObject("LoungeSofa",
                ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 2; i++)
            {
                sofa = new GameObject("LoungeSofa0" + i,
                     ObjectType.Static, RenderType.Opaque);
                sofa.Transform = new Transform(2.9f * Vector3.One,
                 new Vector3(0, 1890.761f + (630.2536f * i), 0), new Vector3(-33 - (7 * i), 1.5f, -25 - (8 * i)));
                sofa.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(sofa);
            }
        }
        //Placed & Textured
        private void InitializeGardenBarrels()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/LargeBarrel");
            var model = Content.Load<Model>("Assets/Models/Garden/LargeBarrel");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var barrel = new GameObject("GardenBarrel",
                ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 3; i++)
            {
                barrel = new GameObject("GardenBarrel0" + i,
                     ObjectType.Static, RenderType.Opaque);
                barrel.Transform = new Transform(0.2f * Vector3.One,
                  new Vector3(0, 28.64789f + (45.83662f * i), 0), new Vector3(18 + (1.4f * i), 0, -56 - (0.2f * i)));
                barrel.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(barrel);
            }
            for (int i = 0; i < 2; i++)
            {
                barrel = new GameObject("GardenBarrelTop0" + i,
                     ObjectType.Static, RenderType.Opaque);
                barrel.Transform = new Transform(0.2f * Vector3.One,
                  new Vector3(0, 0 + (51.5662f * i), 0), new Vector3(18.7f + (1.4f * i), 1.8f, -56 - (0.2f * i)));
                barrel.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(barrel);
            }
        }
        //Placed & Textured
        private void InitializeWC()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/DefaultMaterial_Base_Color");
            var model = Content.Load<Model>("Assets/Models/Garden/medival_WC");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var wc = new GameObject("OutdoorToilet",
                ObjectType.Static, RenderType.Opaque);
            wc.Transform = new Transform(0.19f * Vector3.One,
                new Vector3(0, 1266.237f, 0), new Vector3(65, 0, -63));
            wc.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
            sceneManager.ActiveScene.Add(wc);
        }
        private void InitializeCombatDummy()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/Dummy_BaseColor_compressed");
            var model = Content.Load<Model>("Assets/Models/Garden/CombatDummy");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var dummy = new GameObject("CombatDummy",
                   ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 2; i++)
            {
                dummy = new GameObject("CombatDummy0" + i,
                    ObjectType.Static, RenderType.Opaque);
                dummy.Transform = new Transform(0.14f * Vector3.One,
                new Vector3(0, 11.45916f - (22.91831f * i), 0), new Vector3(64 + (4f * i), 0, -35));
                dummy.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f),
                    mesh));
                sceneManager.ActiveScene.Add(dummy);
            }
        }
        //Placed & Textured
        private void InitializeDoors()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Other/DefaultMaterial_albedo");
            var model = Content.Load<Model>("Assets/Models/MainStructure/anim_door");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var door = new GameObject("KitchenDoorNorth",
                ObjectType.Static, RenderType.Opaque);
            door.Transform = new Transform(new Vector3(0.3f, 0.24f, 0.3f),
                new Vector3(0, 11.45916f, 0), new Vector3(45, 0, -62.2f)); // old y rotation 0.2
            door.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(door);

            var door02 = new GameObject("KitchenDoor",
              ObjectType.Static, RenderType.Opaque);
            door02.Transform = new Transform(new Vector3(0.3f, 0.24f, 0.3f),
                new Vector3(0, 0, 0), new Vector3(11.9f, 0, -56.5f));
            door02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(door02);

            var door03 = new GameObject("HallDoor",
              ObjectType.Static, RenderType.Opaque);
            door03.Transform = new Transform(new Vector3(0.3f, 0.24f, 0.3f),
                new Vector3(0, 630.2536f, 0), new Vector3(-7.7f, 0, -54.1f));
            door03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(door03);

            var door04 = new GameObject("HallDoorInside",
           ObjectType.Static, RenderType.Opaque);
            door04.Transform = new Transform(new Vector3(0.3f, 0.24f, 0.3f),
                new Vector3(0, 0, 0), new Vector3(-21.9f, 0, -58.2f));
            door04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(door04);

            //Main Entrance Door
            var model2 = Content.Load<Model>("Assets/Models/MainStructure/anim_doorOpen");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);
            var mainDoor = new GameObject("MainDoor",
                ObjectType.Static, RenderType.Opaque);
            mainDoor.Transform = new Transform(new Vector3(0.3f, 0.24f, 0.3f),
                new Vector3(0, 0, 0), new Vector3(1.5f, 0, -19.5f));
            mainDoor.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh2));
            sceneManager.ActiveScene.Add(mainDoor);
        }
        //Placed & Textured
        private void InitializeKey()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/key_albedo");
            var model = Content.Load<Model>("Assets/Models/Garden/ancient_key");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var key = new GameObject("KitchenKey",
                ObjectType.Static, RenderType.Opaque);
            key.Transform = new Transform(0.09f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(82.4f, 0.1f, -45.1f));
            key.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(key);
        }
        //Placed & Textured
        private void InitializeBucket()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/OpenOldBucket");
            var model = Content.Load<Model>("Assets/Models/Kitchen/RegularBucketV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var bucket = new GameObject("Bucket",
                ObjectType.Static, RenderType.Opaque);
            bucket.Transform = new Transform(0.09f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(-1.5f, 0.01f, -83.6f));
            bucket.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(bucket);
        }
        //Placed & Textured
        private void InitializeKitchenLight()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/lustreColor");
            var model = Content.Load<Model>("Assets/Models/Kitchen/lustrefbx");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var light = new GameObject("Stove",
                ObjectType.Static, RenderType.Opaque);
            light.Transform = new Transform(0.01f * Vector3.One,
                new Vector3(0, 211.9944f, 0), new Vector3(10, 5, -72f));
            light.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(light);
        }
        //Placed & Textured
        private void InitializeStove()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/Stove_C");
            var model = Content.Load<Model>("Assets/Models/Kitchen/StoveModelV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var stove = new GameObject("Stove",
                ObjectType.Static, RenderType.Opaque);
            stove.Transform = new Transform(0.4f * Vector3.One,
                new Vector3(0, 211.9944f, 0), new Vector3(-3, 0, -82.4f));
            stove.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(stove);
        }
        //Placed, !Textured
        private void InitializeMilk()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Milk");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var milk = new GameObject("Milk",
                ObjectType.Static, RenderType.Opaque);
            milk.Transform = new Transform(0.11f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(5, 1.5f, -86));
            milk.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(milk);
        }
        //Placed, !Textured
        private void InitializeSpoon()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Spoon");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var spoon = new GameObject("Spoon",
                ObjectType.Static, RenderType.Opaque);
            spoon.Transform = new Transform(0.1f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(7, 1.52f, -84.8f));
            spoon.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(spoon);
        }
        //Placed, !Textured
        private void InitializeKitchenPlate()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/PotteryVase");
            var model = Content.Load<Model>("Assets/Models/Kitchen/DeepPlate");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var plate = new GameObject("KitchenPlate",
                ObjectType.Static, RenderType.Opaque);
            plate.Transform = new Transform(0.15f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(6, 1.5f, -85.6f));
            plate.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(plate);
        }
        //Placed, !Textured
        private void InitializeKitchenPot()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/DeepPot");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var pot = new GameObject("KitchenPot",
                ObjectType.Static, RenderType.Opaque);
            pot.Transform = new Transform(0.1f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(8.8f, 1.5f, -85.3f));
            pot.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(pot);
        }
        //Placed, !Textured
        private void InitializeKitchenKnife()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Knife");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var knife = new GameObject("KitchenKnife",
                ObjectType.Static, RenderType.Opaque);
            knife.Transform = new Transform(0.13f * Vector3.One,
                new Vector3(0, 68.75494f, 0), new Vector3(9.9f, 1.55f, -83.9f));
            knife.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DimGray),
                mesh));
            sceneManager.ActiveScene.Add(knife);
        }
        //Placed, !Textured
        private void InitializeKitchenBoard()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Board");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var board = new GameObject("Board",
                ObjectType.Static, RenderType.Opaque);
            board.Transform = new Transform(0.15f * Vector3.One,
                new Vector3(0, 74.48451f, 0), new Vector3(10, 1.54f, -84.2f));
            board.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.BurlyWood),
                mesh));
            sceneManager.ActiveScene.Add(board);
        }
        //Placed, !Textured
        private void InitializeRollingPin()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/RollingPin");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var rollingPin = new GameObject("RollingPin",
                ObjectType.Static, RenderType.Opaque);
            rollingPin.Transform = new Transform(0.09f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(11.3f, 1.6f, -84));
            rollingPin.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.BurlyWood),
                mesh));
            sceneManager.ActiveScene.Add(rollingPin);
        }
        //Placed and textured
        private void InitializeTorches()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/torch");
            var model = Content.Load<Model>("Assets/Models/Garden/FireTorchV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            for (int i = 0; i < 2; i++)
            {
                var torch = new GameObject("TorchFrontGate0" + i,
                ObjectType.Static, RenderType.Opaque);
                torch.Transform = new Transform(0.09f * Vector3.One,
                    new Vector3(0, 1260.507f, 0), new Vector3(8.3f - (5.7f * i), 2, -12.5f));
                torch.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.White),
                    mesh));
                sceneManager.ActiveScene.Add(torch);
            }
            for (int i = 0; i < 2; i++)
            {
                var torch = new GameObject("TorchKitchenGate0" + i,
                ObjectType.Static, RenderType.Opaque);
                torch.Transform = new Transform(0.07f * Vector3.One,
                    new Vector3(0, 1260.507f, 0), new Vector3(13 + (3.7f * i), 2, -53.8f));
                torch.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.White),
                    mesh));
                sceneManager.ActiveScene.Add(torch);
            }
            for (int i = 0; i < 2; i++)
            {
                var torch = new GameObject("TorchHallGate0" + i,
                ObjectType.Static, RenderType.Opaque);
                torch.Transform = new Transform(0.07f * Vector3.One,
                    new Vector3(0, 630.2536f, 0), new Vector3(-3, 2, -58.6f + (3.3f * i)));
                torch.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.White),
                    mesh));
                sceneManager.ActiveScene.Add(torch);
            }
        }
        //Placed & Textured
        private void InitializeGrassModels()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Garden/grassV3");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var model2 = Content.Load<Model>("Assets/Models/Garden/littleGrassV2");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);

            // Model 1
            var grassV1_01 = new GameObject("GrassModel1_1",
                ObjectType.Static, RenderType.Opaque);
            grassV1_01.Transform = new Transform(0.015f * Vector3.One,
                new Vector3(0, 57.29578f, 0), new Vector3(14, 0, -54));
            grassV1_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_01);

            var grassV1_02 = new GameObject("GrassModel1_2",
                ObjectType.Static, RenderType.Opaque);
            grassV1_02.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(16, 0, -55));
            grassV1_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_02);

            var grassV1_03 = new GameObject("GrassModel1_3",
               ObjectType.Static, RenderType.Opaque);
            grassV1_03.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(25, 0, -48));
            grassV1_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_03);

            var grassV1_04 = new GameObject("GrassModel1_4",
               ObjectType.Static, RenderType.Opaque);
            grassV1_04.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 401.0705f, 0), new Vector3(39, 0, -38));
            grassV1_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_04);

            var grassV1_05 = new GameObject("GrassModel1_5",
               ObjectType.Static, RenderType.Opaque);
            grassV1_05.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(47, 0, -59));
            grassV1_05.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_05);

            var grassV1_06 = new GameObject("GrassModel1_6",
               ObjectType.Static, RenderType.Opaque);
            grassV1_06.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(24, 0, -32));
            grassV1_06.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.LightGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_06);

            var grassV1_07 = new GameObject("GrassModel1_7",
               ObjectType.Static, RenderType.Opaque);
            grassV1_07.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(28, 0, -30));
            grassV1_07.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.LightGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_07);

            var grassV1_08 = new GameObject("GrassModel1_8",
               ObjectType.Static, RenderType.Opaque);
            grassV1_08.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(15, 0, -49));
            grassV1_08.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.LightGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_08);

            var grassV1_09 = new GameObject("GrassModel1_9",
              ObjectType.Static, RenderType.Opaque);
            grassV1_09.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(6, 0, -54));
            grassV1_09.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.LightGreen),
                mesh));
            sceneManager.ActiveScene.Add(grassV1_09);

            // Model 2
            var grassV2_01 = new GameObject("GrassModel2_1",
                ObjectType.Static, RenderType.Opaque);
            grassV2_01.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(18, 0, -53));
            grassV2_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_01);

            var grassV2_02 = new GameObject("GrassModel2_2",
                ObjectType.Static, RenderType.Opaque);
            grassV2_02.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 0, 0), new Vector3(19, 0, -50));
            grassV2_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_02);

            var grassV2_03 = new GameObject("GrassModel2_3",
               ObjectType.Static, RenderType.Opaque);
            grassV2_03.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 229.1831f, 0), new Vector3(10, 0, -40));
            grassV2_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_03);

            var grassV2_04 = new GameObject("GrassModel2_4",
               ObjectType.Static, RenderType.Opaque);
            grassV2_04.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 229.1831f, 0), new Vector3(5, 0, -26));
            grassV2_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_04);

            var grassV2_05 = new GameObject("GrassModel2_5",
               ObjectType.Static, RenderType.Opaque);
            grassV2_05.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 57.29578f, 0), new Vector3(0, 0, -30));
            grassV2_05.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_05);

            var grassV2_06 = new GameObject("GrassModel2_6",
               ObjectType.Static, RenderType.Opaque);
            grassV2_06.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 57.29578f, 0), new Vector3(-5, 0, -45));
            grassV2_06.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_06);

            var grassV2_07 = new GameObject("GrassModel2_7",
              ObjectType.Static, RenderType.Opaque);
            grassV2_07.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 57.29578f, 0), new Vector3(-5, 0, -50));
            grassV2_07.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_07);

            var grassV2_08 = new GameObject("GrassModel2_8",
              ObjectType.Static, RenderType.Opaque);
            grassV2_08.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 57.29578f, 0), new Vector3(-4, 0, -49));
            grassV2_08.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_08);

            var grassV2_09 = new GameObject("GrassModel2_9",
              ObjectType.Static, RenderType.Opaque);
            grassV2_09.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 57.29578f, 0), new Vector3(76, 0, -49));
            grassV2_09.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_09);

            var grassV2_010 = new GameObject("GrassModel2_10",
             ObjectType.Static, RenderType.Opaque);
            grassV2_010.Transform = new Transform(0.019f * Vector3.One,
              new Vector3(0, 229.1831f, 0), new Vector3(73, 0, -57));
            grassV2_010.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGreen),
                mesh2));
            sceneManager.ActiveScene.Add(grassV2_010);
        }
        //Placed, !Textured
        private void InitializeShrubs()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/ShrubTexture");
            var model = Content.Load<Model>("Assets/Models/Garden/shrubV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            var shrubV1_01 = new GameObject("ShrubModel01",
                ObjectType.Static, RenderType.Opaque);
            shrubV1_01.Transform = new Transform(0.015f * Vector3.One,
                new Vector3(0, 1890.761f, 0), new Vector3(22.9f, 0, -56.7f));
            shrubV1_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(shrubV1_01);

            var shrubV1_02 = new GameObject("ShrubModel02",
                ObjectType.Static, RenderType.Opaque);
            shrubV1_02.Transform = new Transform(0.019f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(18.6f, 0, -24.2f));
            shrubV1_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
            sceneManager.ActiveScene.Add(shrubV1_02);
        }
        //Placed & Textured
        private void InitializeBoxes()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/Medfan_Boxes_diffuse");
            var model = Content.Load<Model>("Assets/Models/Kitchen/rectBoxV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var model2 = Content.Load<Model>("Assets/Models/Kitchen/SquareBoxV2");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);

            var boxV1_01 = new GameObject("BoxModel01",
                ObjectType.Static, RenderType.Opaque);
            boxV1_01.Transform = new Transform(0.024f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(18, 0.2f, -79));
            boxV1_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.BurlyWood),
                mesh));
            sceneManager.ActiveScene.Add(boxV1_01);

            var boxV1_2 = new GameObject("BoxModel02",
                ObjectType.Static, RenderType.Opaque);
            boxV1_2.Transform = new Transform(0.024f * Vector3.One,
                new Vector3(630.2536f, 171.8873f, 0), new Vector3(19, 1f, -81));
            boxV1_2.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.BurlyWood),
                mesh2));
            sceneManager.ActiveScene.Add(boxV1_2);
        }
        //Placed & Textured
        private void InitializeKitchenChairs()
        {
            var gameObject = new GameObject("KitchenChair",ObjectType.Static, RenderType.Opaque);
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/texture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Chair_02");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("KitchenChair0" + i,
                ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(0.012f * Vector3.One,
                    new Vector3(0, 630.2536f, 0), new Vector3(7 + (2 * i), 0, -70.8f));
                gameObject.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.BurlyWood),
                    mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }

            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("KitchenChairOtherSide0" + i,
                ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(0.012f * Vector3.One,
                    new Vector3(0, 1890.761f, 0), new Vector3(7 + (2 * i), 0, -73.7f));
                gameObject.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.BurlyWood),
                    mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }

            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("KitchenChairV20" + i,
                ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(0.012f * Vector3.One,
                    new Vector3(0, 630.2536f, 0), new Vector3(11 + (2 * i), 0, -70.8f));
                gameObject.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.BurlyWood),
                    mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }

            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("KitchenChairOtherSideV20" + i,
                ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(0.012f * Vector3.One,
                    new Vector3(0, 1890.761f, 0), new Vector3(11 + (2 * i), 0, -73.7f));
                gameObject.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.BurlyWood),
                    mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }
        }
        //Placed & Textured
        private void InitializeKitchenVase()
        {
            // Model - 1
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/PotteryVase");
            var model = Content.Load<Model>("Assets/Models/Kitchen/VaseV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var vase01 = new GameObject("VaseModel01",
            ObjectType.Static, RenderType.Opaque);
            vase01.Transform = new Transform(0.16f * Vector3.One,
                new Vector3(0, 630.2536f, 0), new Vector3(2, 0, -75));
            vase01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(vase01);

            // Model - 2
            var model2 = Content.Load<Model>("Assets/Models/Kitchen/Vase2V2");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);
            var vase02 = new GameObject("VaseModel02",
            ObjectType.Static, RenderType.Opaque);
            vase02.Transform = new Transform(0.19f * Vector3.One,
                new Vector3(0, 630.2536f, 0), new Vector3(5, 0, -79));
            vase02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh2));
            sceneManager.ActiveScene.Add(vase02);
        }

        //Placed & Textured
        private void InitializeCarrige()
        {
            // Main body of cart
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/cart");
            var model = Content.Load<Model>("Assets/Models/Garden/CartV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var carrige1 = new GameObject("CarrigeModel01",
            ObjectType.Static, RenderType.Opaque);
            carrige1.Transform = new Transform(0.19f * Vector3.One,
                new Vector3(0, -166.1578f, 0), new Vector3(31, 0, -28.1f));
            carrige1.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(carrige1);

            // Cart wheels
            var texture2 = Content.Load<Texture2D>("Assets/Textures/Props/Garden/cartWheel");
            var model2 = Content.Load<Model>("Assets/Models/Garden/CartWheel");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);
            var carrigeWheel1 = new GameObject("CarrigeModel01",
            ObjectType.Static, RenderType.Opaque);
            for (int i = 0; i < 2; i++)
            {
                carrigeWheel1 = new GameObject("CarrigeWheel0" + i,
                ObjectType.Static, RenderType.Opaque);
                carrigeWheel1.Transform = new Transform(0.19f * Vector3.One,
                    new Vector3(0, -166.1578f, 0), new Vector3(30.3f + (2.3f * i), 0, -26.9f - (0.5f * i)));
                carrigeWheel1.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture2, 1f, Color.White),
                    mesh2));
                sceneManager.ActiveScene.Add(carrigeWheel1);
            }
            for (int i = 0; i < 2; i++)
            {
                carrigeWheel1 = new GameObject("CarrigeWheelOtherSide0" + i,
                ObjectType.Static, RenderType.Opaque);
                carrigeWheel1.Transform = new Transform(0.19f * Vector3.One,
                    new Vector3(0, 17.18873f, 0), new Vector3(29.9f + (2.3f * i), 0, -28.9f - (0.6f * i)));
                carrigeWheel1.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture2, 1f, Color.White),
                    mesh2));
                sceneManager.ActiveScene.Add(carrigeWheel1);
            }

            //Roller part under cart
            var model3 = Content.Load<Model>("Assets/Models/Garden/CartWheelPart");
            var mesh3 = new Engine.ModelMesh(_graphics.GraphicsDevice, model3);
            var carrigePart1 = new GameObject("CarrigeModel01",
            ObjectType.Static, RenderType.Opaque);
            carrigePart1.Transform = new Transform(0.19f * Vector3.One,
                new Vector3(0, -166.1578f, 0), new Vector3(30.6f, 0, -28));
            carrigePart1.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture2, 1f, Color.White),
                mesh3));
            sceneManager.ActiveScene.Add(carrigePart1);
        }
        //Placed & Textured
        private void InitializeWheetBags()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/OpenLinenBag");
            var texture2 = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/linenBagClosed");
            var model = Content.Load<Model>("Assets/Models/Kitchen/BagOfWheetV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var model2 = Content.Load<Model>("Assets/Models/Kitchen/BagV2");
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model2);

            var wheetBag1 = new GameObject("WheetBagOpen01",
            ObjectType.Static, RenderType.Opaque);
            wheetBag1.Transform = new Transform(0.28f * Vector3.One,
                new Vector3(0, -166.1578f, 0), new Vector3(11, 0, -61.7f));
            wheetBag1.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(wheetBag1);

            var wheetBag2 = new GameObject("WheetBagClosed01",
            ObjectType.Static, RenderType.Opaque);
            wheetBag2.Transform = new Transform(0.27f * Vector3.One,
                new Vector3(0, -166.1578f, 0), new Vector3(3.8f, 0, -61.4f));
            wheetBag2.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture2, 1f, Color.White),
                mesh2));
            sceneManager.ActiveScene.Add(wheetBag2);
        }

        //Placed & Textured
        private void InitializeMug()
        {
            var gameObject = new GameObject("MugModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(0, 57.29578f, 0), new Vector3(9, 1.5f, -71.4f));
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/Mug_BaseColor");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Mug");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);
        }

        // Placed & Textured
        private void InitializeTrees()
        {
            // Tree Model 1 - Deep brown one
            var treeV1_01 = new GameObject("TreeModel01",
                ObjectType.Static, RenderType.Opaque);
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Garden/dryTree");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            treeV1_01.Transform = new Transform(0.07f * Vector3.One,
                new Vector3(0, 859.4367f, 0), new Vector3(23, 0, -45));
            treeV1_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_01);
            var treeV1_02 = new GameObject("TreeModel02",
                ObjectType.Static, RenderType.Opaque);
            treeV1_02.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(20, 0, -30));
            treeV1_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_02);
            var treeV1_03 = new GameObject("TreeModel03",
                ObjectType.Static, RenderType.Opaque);
            treeV1_03.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 458.3662f, 0), new Vector3(48, 0, -57));
            treeV1_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_03);
            var treeV1_04 = new GameObject("TreeModel04",
                ObjectType.Static, RenderType.Opaque);
            treeV1_04.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 57.29578f, 0), new Vector3(89, 0, -57));
            treeV1_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_04);
            var treeV1_05 = new GameObject("TreeModel05",
               ObjectType.Static, RenderType.Opaque);
            treeV1_05.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 171.8873f, 0), new Vector3(59, 0, -60));
            treeV1_05.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_05);
            var treeV1_06 = new GameObject("TreeModel06",
               ObjectType.Static, RenderType.Opaque);
            treeV1_06.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 343.7747f, 0), new Vector3(57, 0, -53));
            treeV1_06.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_06);
            var treeV1_07 = new GameObject("TreeModel07",
               ObjectType.Static, RenderType.Opaque);
            treeV1_07.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 515.662f, 0), new Vector3(44, 0, -32));
            treeV1_07.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_07);
            var treeV1_08 = new GameObject("TreeModel08",
               ObjectType.Static, RenderType.Opaque);
            treeV1_08.Transform = new Transform(0.07f * Vector3.One,
               new Vector3(0, 401.0705f, 0), new Vector3(48, 0, -39));
            treeV1_08.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.SaddleBrown),
                mesh));
            sceneManager.ActiveScene.Add(treeV1_08);

            // Tree Model 2 - Gray one
            var treeV2_01 = new GameObject("TreeModelV201",
                ObjectType.Static, RenderType.Opaque);
            var textureV2 = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var modelV2 = Content.Load<Model>("Assets/Models/Garden/dryFallenBranchesTree");
            var meshV2 = new Engine.ModelMesh(_graphics.GraphicsDevice, modelV2);
            treeV2_01.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 57.29578f, 0), new Vector3(56, 0, -38));
            treeV2_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV2, 1f, Color.Gray),
                meshV2));
            sceneManager.ActiveScene.Add(treeV2_01);
            var treeV2_02 = new GameObject("TreeModelV202",
                ObjectType.Static, RenderType.Opaque);
            treeV2_02.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(39, 0, -54));
            treeV2_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV2, 1f, Color.Gray),
                meshV2));
            sceneManager.ActiveScene.Add(treeV2_02);
            var treeV2_03 = new GameObject("TreeModelV203",
              ObjectType.Static, RenderType.Opaque);
            treeV2_03.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 343.7747f, 0), new Vector3(84, 0, -60));
            treeV2_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV2, 1f, Color.Gray),
                meshV2));
            sceneManager.ActiveScene.Add(treeV2_03);
            var treeV2_04 = new GameObject("TreeModelV204",
             ObjectType.Static, RenderType.Opaque);
            treeV2_04.Transform = new Transform(0.063f * Vector3.One,
                new Vector3(0, 171.8873f, 0), new Vector3(54, 0, -36));
            treeV2_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV2, 1f, Color.Gray),
                meshV2));
            sceneManager.ActiveScene.Add(treeV2_04);

            // Tree Model 3 - Bright brown one
            var treeV3_01 = new GameObject("TreeModelV301",
                ObjectType.Static, RenderType.Opaque);
            var textureV3 = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var modelV3 = Content.Load<Model>("Assets/Models/Garden/drySimpleTree");
            var meshV3 = new Engine.ModelMesh(_graphics.GraphicsDevice, modelV3);
            treeV3_01.Transform = new Transform(0.07f * Vector3.One,
                new Vector3(0, 57.29578f, 0), new Vector3(75, 0, -43));
            treeV3_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV3, 1f, Color.SandyBrown),
                meshV3));
            sceneManager.ActiveScene.Add(treeV3_01);
            var treeV3_02 = new GameObject("TreeModelV302",
            ObjectType.Static, RenderType.Opaque);
            treeV3_02.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(80, 0, -54));
            treeV3_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV3, 1f, Color.SandyBrown),
                meshV3));
            sceneManager.ActiveScene.Add(treeV3_02);
            var treeV3_03 = new GameObject("TreeModelV303",
           ObjectType.Static, RenderType.Opaque);
            treeV3_03.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 458.3662f, 0), new Vector3(50, 0, -58));
            treeV3_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV3, 1f, Color.SandyBrown),
                meshV3));
            sceneManager.ActiveScene.Add(treeV3_03);
            var treeV3_04 = new GameObject("TreeModelV304",
           ObjectType.Static, RenderType.Opaque);
            treeV3_04.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 114.5916f, 0), new Vector3(40, 0, -33));
            treeV3_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV3, 1f, Color.SandyBrown),
                meshV3));
            sceneManager.ActiveScene.Add(treeV3_04);
            var treeV3_05 = new GameObject("TreeModelV305",
          ObjectType.Static, RenderType.Opaque);
            treeV3_05.Transform = new Transform(0.062f * Vector3.One,
                new Vector3(0, 286.4789f, 0), new Vector3(79, 0, -40));
            treeV3_05.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV3, 1f, Color.SandyBrown),
                meshV3));
            sceneManager.ActiveScene.Add(treeV3_05);

            // Tree Model 4 - Pink one
            var treeV4_01 = new GameObject("TreeModelV401",
                ObjectType.Static, RenderType.Opaque);
            var textureV4 = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var modelV4 = Content.Load<Model>("Assets/Models/Garden/dryPurpleTree");
            var meshV4 = new Engine.ModelMesh(_graphics.GraphicsDevice, modelV4);
            treeV4_01.Transform = new Transform(0.07f * Vector3.One,
                new Vector3(0, 1, 0), new Vector3(24, 0, -52));
            treeV4_01.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV4, 1f, Color.RosyBrown),
                meshV4));
            sceneManager.ActiveScene.Add(treeV4_01);
            var treeV4_02 = new GameObject("TreeModelV402",
            ObjectType.Static, RenderType.Opaque);
            treeV4_02.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(77, 0, -60));
            treeV4_02.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV4, 1f, Color.RosyBrown),
                meshV4));
            sceneManager.ActiveScene.Add(treeV4_02);
            var treeV4_03 = new GameObject("TreeModelV403",
          ObjectType.Static, RenderType.Opaque);
            treeV4_03.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 401.0705f, 0), new Vector3(54, 0, -54));
            treeV4_03.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV4, 1f, Color.RosyBrown),
                meshV4));
            sceneManager.ActiveScene.Add(treeV4_03);
            var treeV4_04 = new GameObject("TreeModelV404",
                ObjectType.Static, RenderType.Opaque);
            treeV4_04.Transform = new Transform(0.06f * Vector3.One,
                new Vector3(0, 572.9578f, 0), new Vector3(47, 0, -34));
            treeV4_04.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV4, 1f, Color.RosyBrown),
                meshV4));
            sceneManager.ActiveScene.Add(treeV4_04);
            var treeV4_05 = new GameObject("TreeModelV405",
               ObjectType.Static, RenderType.Opaque);
            treeV4_05.Transform = new Transform(0.062f * Vector3.One,
                new Vector3(0, 229.1831f, 0), new Vector3(84, 0, -41));
            treeV4_05.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(textureV4, 1f, Color.RosyBrown),
                meshV4));
            sceneManager.ActiveScene.Add(treeV4_05);
        }

        //Placed, !Textured
        private void InitializeRock()
        {
            var gameObject = new GameObject("RockModel",
                ObjectType.Static, RenderType.Opaque);
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Garden/rock");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Gray),
                mesh));
            for (int i = 0; i < 3; i++)
            {
                gameObject = new GameObject("Rock0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(0.005f * Vector3.One,
                new Vector3(630.2536f + (114.5916f * i), 0 + (114.5916f * i), 0), new Vector3(34 + (28 * i), 0, -58 + (10 * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.DarkGray),
                mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }
        }
        //Placed, !Textured
        private void InitializeToolbox()
        {
            var gameObject = new GameObject("ToolboxModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.0045f * Vector3.One,
                new Vector3(0, 68.75494f, 0), new Vector3(62, 0.1f, -63));
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Kitchen/Toolbox");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.CornflowerBlue),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);
        }
        //Placed, !Textured
        private void InitializeClipboard()
        {
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Other/Clipboard");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var gameObject = new GameObject("ClipboardModel01",
                ObjectType.Static, RenderType.Opaque);
            //Note outside kitchen door
            gameObject.Transform = new Transform(0.004f * Vector3.One,
                new Vector3(1890.761f, -45.83662f, 0), new Vector3(13.5f, 2.4f, -55.11f));
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Orange),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);

            //Note on table. ONLY THERE AFTER SCREAM FROM KITCHEN

            //var gameObject2 = new GameObject("ClipboardModel02",
            //    ObjectType.Static, RenderType.Opaque);
            //gameObject2.Transform = new Transform(0.004f * Vector3.One,
            //    new Vector3(0, -0.4f, 0), new Vector3(13, 1.48f, -72));
            //gameObject2.AddComponent(new Renderer(
            //    new GDBasicEffect(effect),
            //    new Material(texture, 1f, Color.Orange),
            //    mesh));
            //sceneManager.ActiveScene.Add(gameObject2);
        }
        //Placed, !Textured
        private void InitializeBench()
        {
            var gameObject = new GameObject("BenchModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.04f * Vector3.One,
                new Vector3(0, 257.831f, 0), new Vector3(83, 0, -46));
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Garden/Bench_albedo");
            var model = Content.Load<Model>("Assets/Models/Garden/BenchV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);
        }
        //Placed, !Textured
        private void InitializeTable()
        {
            var gameObject = new GameObject("TableModel",
                ObjectType.Static, RenderType.Opaque);
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Kitchen/tableV2");
            var model = Content.Load<Model>("Assets/Models/Kitchen/BetterTableFixed");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var collider = new Collider(gameObject);
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Table0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(4*Vector3.One,
                new Vector3(0, 452.6367f, 0), new Vector3(8 + (4 * i), 1, -72 - (0.2f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));

                collider = new Collider(gameObject);
                collider.AddPrimitive(new Box(
                        gameObject.Transform.Translation,
                        gameObject.Transform.Rotation,
                        new Vector3(3,1,4)),
                        new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 1);
                gameObject.AddComponent(collider);

                sceneManager.ActiveScene.Add(gameObject);
            }

            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Counter0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(4*Vector3.One,
                new Vector3(0, 435.4479f, 0), new Vector3(6 + (4 * i), 1, -85.8f + (1 * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));

                collider = new Collider(gameObject);
                collider.AddPrimitive(new Box(
                        gameObject.Transform.Translation,
                        gameObject.Transform.Rotation,
                        new Vector3(3, 1, 4)),
                        new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 1);
                gameObject.AddComponent(collider);

                sceneManager.ActiveScene.Add(gameObject);
            }
        }
        //Placed, !Textured
        private void InitializeWell()
        {
            var gameObject = new GameObject("WellModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.6f * Vector3.One,
                new Vector3(0, 343.7747f, 0), new Vector3(38, 0, -44));
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var model = Content.Load<Model>("Assets/Models/Garden/Old_Well");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Silver),
                mesh));
            //var collider = new Collider(gameObject, true);
            //collider.AddPrimitive(new Box(
            //    gameObject.Transform.Translation,
            //    gameObject.Transform.Rotation,
            //    gameObject.Transform.Scale),
            //    new MaterialProperties(0.8f, 0.8f, 0.7f));
            //collider.Enable(gameObject, true, 10);
            //gameObject.AddComponent(collider);

            sceneManager.ActiveScene.Add(gameObject);
        }

        //Character 2D sprite, not in game till the end
        private void InitializeLadyRoesia()
        {
            //var gameObject = new GameObject("Lady Roesia", ObjectType.Static,
            //    RenderType.Transparent);
            //gameObject.Transform = new Transform(new Vector3(2.5f, 3.5f, 1),
            //    null, new Vector3(1.8f, 1.7f, -15));
            //var texture = Content.Load<Texture2D>("Assets/Textures/Character/LadyRoshia_Finished_Fixed");
            //gameObject.AddComponent(new Renderer(
            //    new GDBasicEffect(effect),
            //    new Material(texture, 1),
            //    new QuadMesh(_graphics.GraphicsDevice)));
            //sceneManager.ActiveScene.Add(gameObject);
        }

        //Placed, scaled and textured :)
        private void InitializeTowerModels()
        {
            //Tower Entrance Right
            var gameObject = new GameObject("TowerModelRight",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(new Vector3(0.06f, 0.06f, 0.05f),
                new Vector3(630.25f, 171.9f, 0), new Vector3(13, 15, -18)); // x rot = 11, y rot = 3
            var texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Towers UV New");
            var model = Content.Load<Model>("Assets/Models/MainStructure/TowerV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);            
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);

            //Tower Entrance Left
            gameObject = new GameObject("TowerModelLeft",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(new Vector3(0.06f, 0.06f, 0.05f),
                new Vector3(630.25f, 171.9f, 0), new Vector3(-10, 15, -18));
            texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Towers UV New");
            model = Content.Load<Model>("Assets/Models/MainStructure/TowerV2");
            mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));            
            sceneManager.ActiveScene.Add(gameObject);

            //Tower North
            gameObject = new GameObject("TowerModelNorth",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(new Vector3(0.06f, 0.06f, 0.05f),
                new Vector3(630.25f, -4583.662f, 0), new Vector3(106, 13, -44));
            texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Towers UV New");
            model = Content.Load<Model>("Assets/Models/MainStructure/Tower3");
            mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);
        }

        //Archway models, placed, scaled, textured :)
        private void InitializeEntrance()
        {
            var gameObject = new GameObject("EntranceModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.03f * Vector3.One,
                new Vector3(630.25f, 0, 0), new Vector3(1.5f, 3, -17.5f));
            var texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Towers UV New");
            var model = Content.Load<Model>("Assets/Models/MainStructure/Entrance");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);

            //Entrance to Hall
            var gameObject2 = new GameObject("EntranceModel2",
                ObjectType.Static, RenderType.Opaque);
            gameObject2.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(630.25f, 630.25f, 0), new Vector3(-6, 2, -54));
            var mesh2 = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject2.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh2));
            sceneManager.ActiveScene.Add(gameObject2);

            //Entrance to Kitchen from hall
            var gameObject3 = new GameObject("EntranceModel3",
                ObjectType.Static, RenderType.Opaque);

            gameObject3.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(630.25f, 0, 0), new Vector3(-21.8f, 2, -57.5f));
            var mesh3 = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject3.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh2));

            sceneManager.ActiveScene.Add(gameObject3);

            //Entrance to Kitchen from garden
            var gameObject4 = new GameObject("EntranceModel4",
                ObjectType.Static, RenderType.Opaque);
            gameObject4.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(630.25f, 0, 0), new Vector3(12, 2, -57.5f));
            var mesh4 = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject4.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh4));
            sceneManager.ActiveScene.Add(gameObject4);

            //Entrance to Kitchen from garden door 2
            var gameObject5 = new GameObject("EntranceModel5",
                ObjectType.Static, RenderType.Opaque);
            gameObject5.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(630.25f, 5.729578f, 0), new Vector3(45, 2, -63f));
            var mesh5 = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject5.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh5));
            sceneManager.ActiveScene.Add(gameObject5);

            //Murder window
            var gameObject6 = new GameObject("MurderWindowArch");
            gameObject6.Transform = new Transform(0.025f * Vector3.One,
                new Vector3(630.25f, 1890.761f, 0), new Vector3(-6, 9, -39));
            var mesh6 = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject6.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh6));
            sceneManager.ActiveScene.Add(gameObject6);
            var windowPane = new GameObject("WindowPane",
               ObjectType.Static, RenderType.Opaque);
            windowPane.Transform = new Transform(new Vector3(4, 0.5f, 5),
                new Vector3(630.25f, 1890.761f, 0), new Vector3(-4, 9, -39));
            var texture2 = Content.Load<Texture2D>("Assets/Textures/Foliage/3D/PlainColourTexture");
            var windowMesh = new Engine.CubeMesh(_graphics.GraphicsDevice);
            windowPane.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture2, 1f, Color.White),
                windowMesh));
            sceneManager.ActiveScene.Add(windowPane);
        }
        private void InitializeStairs()
        {
            var gameObject = new GameObject("StairsModel",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(0.02f * Vector3.One,
                new Vector3(630.25f, 1890.761f, 0), new Vector3(-19.1f, 2, -24));
            var texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Set UV");
            var model = Content.Load<Model>("Assets/Models/MainStructure/Stairs");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));
            sceneManager.ActiveScene.Add(gameObject);
        }
        //Placed, scaled, textured, but need to add more for upstairs
        private void InitializeWalls()
        {
            var gameObject = new GameObject("CastleWallNew",
               ObjectType.Static, RenderType.Opaque);
            var model = Content.Load<Model>("Assets/Models/MainStructure/CastleWall_Fixed");
            var smallerModel = Content.Load<Model>("Assets/Models/MainStructure/CastleWall02_Fixed");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            var smallerMesh = new Engine.ModelMesh(_graphics.GraphicsDevice, smallerModel);
            var texture = Content.Load<Texture2D>("Assets/Textures/Walls/Castle Towers UV New");
            var collider = new Collider(gameObject);

            //Left side
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Castle wall front left 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    Vector3.Zero,
                    new Vector3(-17 - (19.3f * i), 6.9f, -20));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }

            //South side
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Castle wall south side 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    new Vector3(0, 630.25f, 0),
                    new Vector3(-43.8f, 6.9f, -30.8f - (18.9f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //South west side
            for (int i = 0; i < 3; i++)
            {
                gameObject = new GameObject("Castle wall south west side 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    new Vector3(0, -1948.057f, 0),
                    new Vector3(-36.4f + (16 * i), 7, -65 - (10 * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //Right side
            for (int i = 0; i < 5; i++)
            {
                gameObject = new GameObject("Castle wall front right 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    new Vector3(0, 10.313f, 0),
                    new Vector3(14 + (18.5f * i), 7, -21 - (3.5f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //North west side
            for (int i = 0; i < 5; i++)
            {
                gameObject = new GameObject("Castle wall north west side 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    new Vector3(0, 343.77f, 0),
                    new Vector3(12.4f + (18 * i), 7, -87 + (5f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //North side
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle wall north side 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    new Vector3(0, 458.37f, 0),
                    new Vector3(94 + (2.5f * i), 7, -58 + (18f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19.4f, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }

            //Above entrance
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle wall above entrance 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(20, 39, 19),
                    Vector3.Zero,
                    new Vector3(-3, 12, -17));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }

            //Above kitchen entrance
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Castle wall above entrance kitchen 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(6, 26.5f, 15),
                    new Vector3(0, 5.73f, 0),
                    new Vector3(11.7f + (33.8f * i), 8.9f, -57.5f - (4.9f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //Above Main Hall Entrance
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle wall above entrance hall 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(6, 26.8f, 17),
                    new Vector3(0, 630.25f, 0),
                    new Vector3(-5.9f, 8.8f, -53.5f));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //Above hall to kitchen entrance
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle wall above entrance hall to kitchen 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(6, 27, 15),
                    Vector3.Zero,
                    new Vector3(-22, 8.9f, -57.9f));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //Inside hall
            for (int i = 0; i < 3; i++)
            {
                gameObject = new GameObject("Castle hall wall lol 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(12, 39, 19),
                    new Vector3(0, 269.29f, 0),
                    new Vector3(-6.1f + (0.12f * i), 7, -24 - (11.2f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                smallerMesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(12, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            //Inside hall South 
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Castle hall wall south 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(19.8f, 39, 19),
                    Vector3.Zero,
                    new Vector3(-33 + (22.7f * i), 7, -58));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }

            // Kitchen wall
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle hall kitchen 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(12, 39, 19),
                    new Vector3(0, 177.62f, 0),
                    new Vector3(5.1f, 7.7f, -57.5f));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                smallerMesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(11, 8, 3.4f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            // kitchen walls, north west
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle hall kitchen NW 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(19.7f, 39, 20),
                    new Vector3(0, -171.89f, 0),
                    new Vector3(23, 6.9f, -59.4f));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19, 8, 3.9f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
            
            // North-East inside
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("Castle hall kitchen NW 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(11.8f, 42, 19),
                    new Vector3(0, -171.89f - (177.616f * i), 0),
                    new Vector3(37.9f + (14.8f * i), 7, -61.2f - (3.1f * i)));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                smallerMesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(10+(1.2f*i), 8, 3.5f+(0.8f*i))),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }

            // North-most inside wall kitchen
            for (int i = 0; i < 1; i++)
            {
                gameObject = new GameObject("Castle hall kitchen NW 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(
                    new Vector3(19.7f, 39, 20),
                    new Vector3(0, -171.8873f, 0),
                    new Vector3(66, 7, -66));
                gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f),
                mesh));
                //add Collision Surface(s)
                collider = new Collider(gameObject, true);
                collider.AddPrimitive(new Box(
                    gameObject.Transform.Translation,
                    gameObject.Transform.Rotation,
                    new Vector3(19, 8, 3.6f)),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
                collider.Enable(gameObject, true, 50);
                gameObject.AddComponent(collider);
                sceneManager.ActiveScene.Add(gameObject);
            }
        }

        // Modelled kitchen floor in maya using screen shot of room as ref. Texture not looking great
        // as when I brought in a tiled texture it looks blurry. See if this can be fixed soon. Same issue
        // with the grass texture.
        private void InitializeFloors()
        {
            var gameObject = new GameObject("KitchenFloorModel",
              ObjectType.Static, RenderType.Opaque);
            var texture = Content.Load<Texture2D>("Assets/Textures/Floors/DarkWoodFloor_Tiled04");
            var model = Content.Load<Model>("Assets/Models/MainStructure/KitchenFloorV2");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            //Ceiling in kitchen and kitchen floor
            for (int i = 0; i < 2; i++)
            {
                gameObject = new GameObject("KitchenFloorModel" + i,
                    ObjectType.Static, RenderType.Opaque);
                gameObject.Transform = new Transform(new Vector3(18.5f, 2, 18.5f),
                    new Vector3(0, 0, 0), new Vector3(11, 0.1f + (5 * i), -91.6f));
                gameObject.AddComponent(new Renderer(
                    new GDBasicEffect(litEffect),
                    new Material(texture, 1f, Color.White),
                    mesh));
                sceneManager.ActiveScene.Add(gameObject);
            }

            //Main Hall
            var gameObject2 = new GameObject("MainHallFloor",
              ObjectType.Static, RenderType.Opaque);
            var texture2 = Content.Load<Texture2D>("Assets/Textures/Floors/Medieval_Floor");
            var mesh2 = new Engine.CubeMesh(_graphics.GraphicsDevice);
            for (int i = 0; i < 2; i++)
            {
                gameObject2 = new GameObject("Castle hall main hall 0" + i, ObjectType.Static, RenderType.Opaque);
                gameObject2.Transform = new Transform(new Vector3(34.7f, 0.1f, 38f),
                  new Vector3(0, 0, 0), new Vector3(-25, 0 + (6 * i), -40));
                gameObject2.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture2, 1f, Color.White),
                mesh2));
                sceneManager.ActiveScene.Add(gameObject2);
            }
        }

        #endregion

        private void InitializeXYZ()
        {
            //  throw new NotImplementedException();
        }

        private void InitializeCollidableGround(float worldScale)
        {
            var gdBasicEffect = new GDBasicEffect(unlitEffect);
            var quadMesh = new QuadMesh(_graphics.GraphicsDevice);

            //ground
            var ground = new GameObject("ground");
            ground.Transform = new Transform(new Vector3(worldScale, worldScale, 1),
                new Vector3(-90, 0, 0), new Vector3(0, 0, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/Foliage/2D/Ground/grass1");
            ground.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1), quadMesh));

            //add Collision Surface(s)
            var collider = new Collider(ground);
            collider.AddPrimitive(new Box(
                    ground.Transform.Translation,
                    ground.Transform.Rotation,
                    ground.Transform.Scale),
                    new MaterialProperties(0.8f, 0.8f, 0.7f));
            collider.Enable(ground, true, 1);
            ground.AddComponent(collider);

            sceneManager.ActiveScene.Add(ground);
        }

        private void InitializeCollidableBox()
        {
            //game object
            var gameObject = new GameObject("my first collidable box!", ObjectType.Dynamic, RenderType.Opaque);
            gameObject.GameObjectType = GameObjectType.Collectible;

            gameObject.Transform = new Transform(
                new Vector3(1, 1, 1),
                new Vector3(45, 45, 0),
                new Vector3(0, 15, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Crates/crate2");
            var model = Content.Load<Model>("Assets/Models/DemoModels/cube");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1, Color.White),
                mesh));

            var collider = new Collider(gameObject, true);
            collider.AddPrimitive(new Box(
                gameObject.Transform.Translation,
                gameObject.Transform.Rotation,
                gameObject.Transform.Scale), //make the colliders a fraction larger so that transparent boxes dont sit exactly on the ground and we end up with flicker or z-fighting
                new MaterialProperties(0.8f, 0.8f, 0.7f));
            collider.Enable(gameObject, false, 10);
            gameObject.AddComponent(collider);

            //var collider = new Collider(gameObject);
            //collider.AddPrimitive(new Sphere(
            //    gameObject.Transform.Translation, 1), //make the colliders a fraction larger so that transparent boxes dont sit exactly on the ground and we end up with flicker or z-fighting
            //    new MaterialProperties(0.2f, 0.8f, 0.7f));
            //collider.Enable(gameObject, true, 10);
            //gameObject.AddComponent(collider);

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeCollidableHighDetailMonkey()
        {
            //game object
            var gameObject = new GameObject("my first collidable high detail monkey!", ObjectType.Static, RenderType.Opaque);
            gameObject.GameObjectType = GameObjectType.Consumable;

            //TODO - rotation on triangle mesh not working
            gameObject.Transform = new Transform(
                new Vector3(1, 1, 1),
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Props/Crates/crate2");
            var model = Content.Load<Model>("Assets/Models/DemoModels/monkey");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.Yellow),
                mesh));

            var model_medium = Content.Load<Model>("Assets/Models/DemoModels/monkey_medium");
            var collider = new Collider(gameObject);
            collider.AddPrimitive(CollisionUtility.GetTriangleMesh(model_medium,
                gameObject.Transform), new MaterialProperties(0.8f, 0.8f, 0.7f));

            //NOTE - TriangleMesh colliders MUST be marked as IMMOVABLE=TRUE
            collider.Enable(gameObject, true, 1);
            gameObject.AddComponent(collider);

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeDemoModel()
        {
            //game object
            var gameObject = new GameObject("my first bottle!",
                ObjectType.Static, RenderType.Opaque);

            gameObject.Transform = new Transform(0.0005f * Vector3.One,
                new Vector3(-90, 0, 0), new Vector3(2, 0, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Props/Crates/crate2");

            var model = Content.Load<Model>("Assets/Models/DemoModels/bottle2");

            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeRadarModel()
        {
            //game object
            var gameObject = new GameObject("radar",
                ObjectType.Static, RenderType.Opaque);

            gameObject.Transform = new Transform(0.005f * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(8, 0, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Props/Crates/crate2");

            var model = Content.Load<Model>("Assets/Models/DemoModels/radar-display");

            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeDemoButton()
        {
            //game object
            var gameObject = new GameObject("my first button!",
                ObjectType.Static, RenderType.Opaque);

            gameObject.Transform = new Transform(6 * Vector3.One,
                new Vector3(0, 0, 0), new Vector3(-10, -5, 0));
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Button/button_DefaultMaterial_Base_color");

            var model = Content.Load<Model>("Assets/Models/DemoModels/button");

            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(litEffect),
                new Material(texture, 1f, Color.White),
                mesh));

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeDemoQuad()
        {
            //game object
            var gameObject = new GameObject("my first quad",
                ObjectType.Static, RenderType.Opaque);
            gameObject.Transform = new Transform(null, null,
                new Vector3(-2, 1, 0));  //World
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Props/Crates/crate1");
            gameObject.AddComponent(new Renderer(new GDBasicEffect(litEffect),
                new Material(texture, 1), new QuadMesh(_graphics.GraphicsDevice)));

            gameObject.AddComponent(new SimpleRotationBehaviour(new Vector3(1, 0, 0), 5 / 60.0f));

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeTreeQuad()
        {
            //game object
            var gameObject = new GameObject("my first tree", ObjectType.Static,
                RenderType.Transparent);
            gameObject.Transform = new Transform(new Vector3(3, 3, 1), null, new Vector3(-6, 1.5f, 1));  //World
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Foliage/Trees/tree1");
            gameObject.AddComponent(new Renderer(
                new GDBasicEffect(unlitEffect),
                new Material(texture, 1),
                new QuadMesh(_graphics.GraphicsDevice)));

            //a weird tree that makes sounds
            gameObject.AddComponent(new AudioEmitterBehaviour());

            sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializePlayer()
        {
            playerGameObject = new GameObject("player 1", ObjectType.Static, RenderType.Opaque);

            playerGameObject.Transform = new Transform(new Vector3(0.4f, 0.4f, 1),
                null, new Vector3(0, 0.2f, -2));
            var texture = Content.Load<Texture2D>("Assets/Textures/Props/Crates/crate2");
            var model = Content.Load<Model>("Assets/Models/sphere");
            var mesh = new Engine.ModelMesh(_graphics.GraphicsDevice, model);

            playerGameObject.AddComponent(new Renderer(new GDBasicEffect(litEffect),
                new Material(texture, 1),
                mesh));

            playerGameObject.AddComponent(new PlayerController(AppData.FIRST_PERSON_MOVE_SPEED, AppData.FIRST_PERSON_STRAFE_SPEED,
                AppData.PLAYER_ROTATE_SPEED_VECTOR2, true));

            sceneManager.ActiveScene.Add(playerGameObject);

            //set this as active player
            Application.Player = playerGameObject;
        }

        private void InitializeSkyBox(float worldScale)
        {
            float halfWorldScale = worldScale / 2.0f;

            GameObject quad = null;
            var gdBasicEffect = new GDBasicEffect(unlitEffect);
            var quadMesh = new QuadMesh(_graphics.GraphicsDevice);

            //skybox - back face
            quad = new GameObject("skybox back face");
            quad.Transform = new Transform(new Vector3(worldScale, worldScale, 1), null, new Vector3(0, 0, -halfWorldScale));
            var texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Skybox/back");
            quad.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1, Color.DarkBlue), quadMesh));
            sceneManager.ActiveScene.Add(quad);

            //skybox - left face
            quad = new GameObject("skybox left face");
            quad.Transform = new Transform(new Vector3(worldScale, worldScale, 1),
                new Vector3(0, 90, 0), new Vector3(-halfWorldScale, 0, 0));
            texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Skybox/left");
            quad.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1, Color.DarkBlue), quadMesh));
            sceneManager.ActiveScene.Add(quad);

            //skybox - right face
            quad = new GameObject("skybox right face");
            quad.Transform = new Transform(new Vector3(worldScale, worldScale, 1),
                new Vector3(0, -90, 0), new Vector3(halfWorldScale, 0, 0));
            texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Skybox/right");
            quad.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1, Color.DarkBlue), quadMesh));
            sceneManager.ActiveScene.Add(quad);

            //skybox - top face
            quad = new GameObject("skybox top face");
            quad.Transform = new Transform(new Vector3(worldScale, worldScale, 1),
                new Vector3(90, -90, 0), new Vector3(0, halfWorldScale, 0));
            texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Skybox/sky");
            quad.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1, Color.DarkBlue), quadMesh));
            sceneManager.ActiveScene.Add(quad);

            //skybox - front face
            quad = new GameObject("skybox front face");
            quad.Transform = new Transform(new Vector3(worldScale, worldScale, 1),
                new Vector3(0, -180, 0), new Vector3(0, 0, halfWorldScale));
            texture = Content.Load<Texture2D>("Assets/Textures/DemoTextures/Skybox/front");
            quad.AddComponent(new Renderer(gdBasicEffect, new Material(texture, 1, Color.DarkBlue), quadMesh));
            sceneManager.ActiveScene.Add(quad);
        }

        #endregion Actions - Level Specific

        #region Actions - Engine Specific

        private void InitializeEngine(Vector2 resolution, bool isMouseVisible, bool isCursorLocked)
        {
            //add support for mouse etc
            InitializeInput();

            //add game effects
            InitializeEffects();

            //add dictionaries to store and access content
            InitializeDictionaries();

            //add camera, scene manager
            InitializeManagers();

            //share some core references
            InitializeGlobals();

            //set screen properties (incl mouse)
            InitializeScreen(resolution, isMouseVisible, isCursorLocked);

            //add game cameras
            InitializeCameras();
        }

        private void InitializeGlobals()
        {
            //Globally shared commonly accessed variables
            Application.Main = this;
            Application.GraphicsDeviceManager = _graphics;
            Application.GraphicsDevice = _graphics.GraphicsDevice;
            Application.Content = Content;

            //Add access to managers from anywhere in the code
            Application.CameraManager = cameraManager;
            Application.SceneManager = sceneManager;
            Application.SoundManager = soundManager;
            Application.PhysicsManager = physicsManager;

            Application.UISceneManager = hudManager;
        }

        private void InitializeInput()
        {
            //Globally accessible inputs
            Input.Keys = new KeyboardComponent(this);
            Components.Add(Input.Keys);
            Input.Mouse = new MouseComponent(this);
            Components.Add(Input.Mouse);
            Input.Gamepad = new GamepadComponent(this);
            Components.Add(Input.Gamepad);
        }

        /// <summary>
        /// Sets game window dimensions and shows/hides the mouse
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="isMouseVisible"></param>
        /// <param name="isCursorLocked"></param>
        private void InitializeScreen(Vector2 resolution, bool isMouseVisible, bool isCursorLocked)
        {
            Screen screen = new Screen();

            //set resolution
            screen.Set(resolution, isMouseVisible, isCursorLocked);

            //set global for re-use by other entities
            Application.Screen = screen;

            //set starting mouse position i.e. set mouse in centre at startup
            Input.Mouse.Position = screen.ScreenCentre;

            ////calling set property
            //_graphics.PreferredBackBufferWidth = (int)resolution.X;
            //_graphics.PreferredBackBufferHeight = (int)resolution.Y;
            //IsMouseVisible = isMouseVisible;
            //_graphics.ApplyChanges();
        }

        private void InitializeManagers()
        {
            //add event dispatcher for system events - the most important element!!!!!!
            eventDispatcher = new EventDispatcher(this);
            //add to Components otherwise no Update() called
            Components.Add(eventDispatcher);

            //add support for multiple cameras and camera switching
            cameraManager = new CameraManager(this);
            //add to Components otherwise no Update() called
            Components.Add(cameraManager);

            //big kahuna nr 1! this adds support to store, switch and Update() scene contents
            sceneManager = new SceneManager<Scene>(this);
            //add to Components otherwise no Update()
            Components.Add(sceneManager);

            //big kahuna nr 2! this renders the ActiveScene from the ActiveCamera perspective
            renderManager = new RenderManager(this, new ForwardSceneRenderer(_graphics.GraphicsDevice));
            renderManager.DrawOrder = 1;
            Components.Add(renderManager);

            //add support for playing sounds
            soundManager = new SoundManager();
            //why don't we add SoundManager to Components? Because it has no Update()
            //wait...SoundManager has no update? Yes, playing sounds is handled by an internal MonoGame thread - so we're off the hook!

            //add the physics manager update thread
            physicsManager = new PhysicsManager(this, AppData.GRAVITY);
            Components.Add(physicsManager);

            #region Collision - Picking

            //picking support using physics engine
            //this predicate lets us say ignore all the other collidable objects except interactables and consumables
            Predicate<GameObject> collisionPredicate =
                (collidableObject) =>
                {
                    if (collidableObject != null)
                        return collidableObject.GameObjectType
                        == GameObjectType.Interactable
                        || collidableObject.GameObjectType == GameObjectType.Consumable
                        || collidableObject.GameObjectType == GameObjectType.Collectible;
                    return false;
                };

            pickingManager = new PickingManager(this,
                AppData.PICKING_MIN_PICK_DISTANCE,
                AppData.PICKING_MAX_PICK_DISTANCE,
                collisionPredicate);
            Components.Add(pickingManager);

            #endregion

            #region Game State

            //add state manager for inventory and countdown
            stateManager = new StateManager(this, AppData.MAX_GAME_TIME_IN_MSECS);
            Components.Add(stateManager);

            #endregion

            #region UI

            hudManager = new UserInterfaceManager(this);
            hudManager.StatusType = StatusType.Off;
            Components.Add(hudManager);

            hudRenderManager = new Render2DManager(this, _spriteBatch, hudManager);
            hudRenderManager.StatusType = StatusType.Off;
            hudRenderManager.DrawOrder = 2;
            Components.Add(hudRenderManager);

            #endregion

            #region Menu

            menuManager = new UserInterfaceManager(this);
            menuManager.StatusType = StatusType.Off;
            Components.Add(menuManager);

            menuRenderManager = new Render2DManager(this, _spriteBatch, menuManager);
            menuRenderManager.StatusType = StatusType.Off;
            menuRenderManager.DrawOrder = 3;
            Components.Add(menuRenderManager);

            #endregion
        }

        private void InitializeDictionaries()
        {
            //TODO - add texture dictionary, soundeffect dictionary, model dictionary
        }

        private void InitializeDebug(bool showCollisionSkins = true)
        {
            //intialize the utility component
            var perfUtility = new PerfUtility(this, _spriteBatch,
                new Vector2(10, 10),
                new Vector2(0, 22));

            //set the font to be used
            var spriteFont = Content.Load<SpriteFont>("Assets/Fonts/Perf");

            //add components to the info list to add UI information
            float headingScale = 1f;
            float contentScale = 0.9f;
            perfUtility.infoList.Add(new TextInfo(_spriteBatch, spriteFont, "Performance ------------------------------", Color.Yellow, headingScale * Vector2.One));
            perfUtility.infoList.Add(new FPSInfo(_spriteBatch, spriteFont, "FPS:", Color.White, contentScale * Vector2.One));
            perfUtility.infoList.Add(new TextInfo(_spriteBatch, spriteFont, "Camera -----------------------------------", Color.Yellow, headingScale * Vector2.One));
            perfUtility.infoList.Add(new CameraNameInfo(_spriteBatch, spriteFont, "Name:", Color.White, contentScale * Vector2.One));

            var infoFunction = (Transform transform) =>
            {
                return transform.Translation.GetNewRounded(1).ToString();
            };

            perfUtility.infoList.Add(new TransformInfo(_spriteBatch, spriteFont, "Pos:", Color.White, contentScale * Vector2.One,
                ref Application.CameraManager.ActiveCamera.transform, infoFunction));

            infoFunction = (Transform transform) =>
            {
                return transform.Rotation.GetNewRounded(1).ToString();
            };

            perfUtility.infoList.Add(new TransformInfo(_spriteBatch, spriteFont, "Rot:", Color.White, contentScale * Vector2.One,
                ref Application.CameraManager.ActiveCamera.transform, infoFunction));

            perfUtility.infoList.Add(new TextInfo(_spriteBatch, spriteFont, "Object -----------------------------------", Color.Yellow, headingScale * Vector2.One));
            perfUtility.infoList.Add(new ObjectInfo(_spriteBatch, spriteFont, "Objects:", Color.White, contentScale * Vector2.One));
            perfUtility.infoList.Add(new TextInfo(_spriteBatch, spriteFont, "Hints -----------------------------------", Color.Yellow, headingScale * Vector2.One));
            perfUtility.infoList.Add(new TextInfo(_spriteBatch, spriteFont, "Use mouse scroll wheel to change security camera FOV, F1-F4 for camera switch", Color.White, contentScale * Vector2.One));

            //add to the component list otherwise it wont have its Update or Draw called!
            // perfUtility.StatusType = StatusType.Drawn | StatusType.Updated;
            perfUtility.DrawOrder = 3;
            Components.Add(perfUtility);

            if (showCollisionSkins)
            {
                var physicsDebugDrawer = new PhysicsDebugDrawer(this);
                physicsDebugDrawer.DrawOrder = 4;
                Components.Add(physicsDebugDrawer);
            }
        }

        #endregion Actions - Engine Specific

        #region Actions - Update, Draw

        protected override void Update(GameTime gameTime)
        {
            //dummy key strokes
            //Raise

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (cameraManager.activeCamera.transform.translation.Z <= -56)
                if (Input.Keys.IsPressed(Keys.W) || Input.Keys.IsPressed(Keys.A) || Input.Keys.IsPressed(Keys.S) || Input.Keys.IsPressed(Keys.D)
                    || Input.Keys.IsPressed(Keys.Up) || Input.Keys.IsPressed(Keys.Down) || Input.Keys.IsPressed(Keys.Left) || Input.Keys.IsPressed(Keys.Right))
                {
                    Application.SoundManager.Resume("boom3");
                    Application.SoundManager.Pause("boom1");
                    Application.SoundManager.Pause("boom2");
                }
                else
                    Application.SoundManager.Pause("boom3");
            else if (cameraManager.activeCamera.transform.translation.Z >= -56 && cameraManager.activeCamera.transform.translation.Z <= -20 && cameraManager.activeCamera.transform.translation.X <= -7)
                if (Input.Keys.IsPressed(Keys.W) || Input.Keys.IsPressed(Keys.A) || Input.Keys.IsPressed(Keys.S) || Input.Keys.IsPressed(Keys.D)
                    || Input.Keys.IsPressed(Keys.Up) || Input.Keys.IsPressed(Keys.Down) || Input.Keys.IsPressed(Keys.Left) || Input.Keys.IsPressed(Keys.Right))
                {
                    Application.SoundManager.Resume("boom2");
                    Application.SoundManager.Pause("boom1");
                    Application.SoundManager.Pause("boom3");
                }
                else
                    Application.SoundManager.Pause("boom2");

            else
                if (Input.Keys.IsPressed(Keys.W) || Input.Keys.IsPressed(Keys.A) || Input.Keys.IsPressed(Keys.S) || Input.Keys.IsPressed(Keys.D)
                    || Input.Keys.IsPressed(Keys.Up) || Input.Keys.IsPressed(Keys.Down) || Input.Keys.IsPressed(Keys.Left) || Input.Keys.IsPressed(Keys.Right))
            {
                Application.SoundManager.Resume("boom1");
                Application.SoundManager.Pause("boom3");
                Application.SoundManager.Pause("boom2");
            }
            else
                Application.SoundManager.Pause("boom1");

#if DEMO
            if (Input.Keys.WasJustPressed(Keys.P))
            {
                EventDispatcher.Raise(new EventData(EventCategoryType.Menu,
                    EventActionType.OnPause));
            }
            else if (Input.Keys.WasJustPressed(Keys.U))
            {
                EventDispatcher.Raise(new EventData(EventCategoryType.Menu,
                   EventActionType.OnPlay));
            }

            if (Input.Keys.WasJustPressed(Keys.B))
            {
                object[] parameters = { "boom1" };
                EventDispatcher.Raise(
                    new EventData(EventCategoryType.Player,
                    EventActionType.OnWin,
                    parameters));

                //    Application.SoundManager.Play2D("boom1");
            }

            #region Demo - Camera switching

            if (Input.Keys.IsPressed(Keys.F1))
                cameraManager.SetActiveCamera(AppData.FIRST_PERSON_CAMERA_NAME);
            else if (Input.Keys.IsPressed(Keys.F2))
                cameraManager.SetActiveCamera(AppData.SECURITY_CAMERA_NAME);
            else if (Input.Keys.IsPressed(Keys.F3))
                cameraManager.SetActiveCamera(AppData.CURVE_CAMERA_NAME);
            else if (Input.Keys.IsPressed(Keys.F4))
                cameraManager.SetActiveCamera(AppData.THIRD_PERSON_CAMERA_NAME);

            #endregion Demo - Camera switching

            #region Demo - Gamepad

            var thumbsL = Input.Gamepad.ThumbSticks(false);
            //   System.Diagnostics.Debug.WriteLine(thumbsL);

            var thumbsR = Input.Gamepad.ThumbSticks(false);
            //     System.Diagnostics.Debug.WriteLine(thumbsR);

            //    System.Diagnostics.Debug.WriteLine($"A: {Input.Gamepad.IsPressed(Buttons.A)}");

            #endregion Demo - Gamepad

            #region Demo - Raising events using GDEvent

            if (Input.Keys.WasJustPressed(Keys.E))
                OnChanged.Invoke(this, null); //passing null for EventArgs but we'll make our own class MyEventArgs::EventArgs later

            #endregion

#endif
            //fixed a bug with components not getting Update called
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }

        #endregion Actions - Update, Draw
    }
}