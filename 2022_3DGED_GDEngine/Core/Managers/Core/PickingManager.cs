using GD.App;
using GD.Engine.Events;
using GD.Engine.Globals;
using Microsoft.Xna.Framework;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using System;

namespace GD.Engine
{
    public class PickingManager : PausableGameComponent
    {
        private float pickStartDistance;
        private float pickEndDistance;
        private Predicate<GameObject> collisionPredicate;
        private GameObject pickedObject;

        public PickingManager(Game game,
           float pickStartDistance, float pickEndDistance,
           Predicate<GameObject> collisionPredicate)
           : base(game)
        {
            this.pickStartDistance = pickStartDistance;
            this.pickEndDistance = pickEndDistance;
            this.collisionPredicate = collisionPredicate;
        }

        public override void Update(GameTime gameTime)
        {
            if (IsUpdated)
                HandleMouse(gameTime);

            base.Update(gameTime);
        }

        protected virtual void HandleMouse(GameTime gameTime)
        {
            if (Input.Mouse.WasJustClicked(Inputs.MouseButton.Left) || Input.Keys.WasJustPressed(Keys.E)) 
                GetPickedObject();

            //predicate was matched and i should notify
            if (pickedObject != null)
            {
                object[] parameters = { pickedObject };
                EventDispatcher.Raise(new EventData(EventCategoryType.Picking,
                    EventActionType.OnObjectPicked, parameters));

                if (Input.Mouse.WasJustClicked(Inputs.MouseButton.Left) || Input.Keys.IsPressed(Keys.E))
                {
                    if (pickedObject.Name == "KitchenKey")
                    {
                        EventDispatcher.Raise(new EventData(EventCategoryType.GameObject,
                        EventActionType.OnRemoveObject, parameters));
                    }
                    else if (pickedObject.Name == "KitchenDoorClosed")
                    {
                        EventDispatcher.Raise(new EventData(EventCategoryType.GameObject,
                        EventActionType.OnDoorOpen, parameters));                        
                    }
                }
            }
            else
            {
                EventDispatcher.Raise(new EventData(EventCategoryType.Picking,
                 EventActionType.OnNoObjectPicked));
            }
        }

        public void GetPickedObject()
        {
            Vector3 pos;
            Vector3 normal;

            pickedObject =
                Input.Mouse.GetPickedObject(Application.CameraManager.ActiveCamera,
                Application.CameraManager.ActiveCameraTransform,
                pickStartDistance, pickEndDistance,
                out pos, out normal) as GameObject;

            if (pickedObject != null && collisionPredicate(pickedObject))
            {
                //TODO - here is where you decide what to do!
                System.Diagnostics.Debug.WriteLine(pickedObject.GameObjectType);

                var behaviour = pickedObject.GetComponent<PickupBehaviour>();

                if (behaviour != null)
                   System.Diagnostics.Debug.WriteLine($"{behaviour.Desc} - {behaviour.Value}");


                object[] parameters = { pickedObject };

                if (pickedObject.Name == "KitchenKey")
                {
                    ////OnPickup
                    EventDispatcher.Raise(new EventData(
                        EventCategoryType.GameObject,
                        EventActionType.OnRemoveObject,
                        parameters));
                    System.Diagnostics.Debug.WriteLine($"{pickedObject.Name} ");
                }
                else if(pickedObject.Name == "KitchenDoorClosed")
                { 
                    //Open door
                    EventDispatcher.Raise(new EventData(
                        EventCategoryType.GameObject,
                        EventActionType.OnDoorOpen,
                        parameters));
                    System.Diagnostics.Debug.WriteLine($"{pickedObject.Name} ");
                }               

            }
            else
                pickedObject = null;
        }
    }
}