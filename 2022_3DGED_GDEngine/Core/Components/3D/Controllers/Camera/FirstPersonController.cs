﻿using GD.Engine.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace GD.Engine
{
    /// <summary>
    /// Adds simple non-collidable 1st person controller to camera using keyboard and mouse input
    /// </summary>
    public class FirstPersonController : Component
    {
        #region Fields

        protected float moveSpeed = 0.05f;
        protected float strafeSpeed = 0.025f;
        protected Vector2 rotationSpeed;
        private bool isGrounded;

        #endregion Fields

        #region Temps

        protected Vector3 translation = Vector3.Zero;
        protected Vector3 rotation = Vector3.Zero;

        #endregion Temps

        #region Constructors

        public FirstPersonController(float moveSpeed, float strafeSpeed, float rotationSpeed, bool isGrounded = true)
    : this(moveSpeed, strafeSpeed, rotationSpeed * Vector2.One, isGrounded)
        {
        }

        public FirstPersonController(float moveSpeed, float strafeSpeed, Vector2 rotationSpeed, bool isGrounded = true)
        {
            this.moveSpeed = moveSpeed;
            this.strafeSpeed = strafeSpeed;
            this.rotationSpeed = rotationSpeed;
            this.isGrounded = isGrounded;
        }

        #endregion Constructors

        #region Actions - Update, Input

        public override void Update(GameTime gameTime)
        {
            HandleMouseInput(gameTime);
            HandleKeyboardInput(gameTime);
        }

        protected virtual void HandleKeyboardInput(GameTime gameTime)
        {
            translation = Vector3.Zero;

            if (Input.Keys.IsPressed(Keys.W) || Input.Keys.IsPressed(Keys.Up))
                translation += transform.World.Forward * moveSpeed * gameTime.ElapsedGameTime.Milliseconds;
            else if (Input.Keys.IsPressed(Keys.S) || Input.Keys.IsPressed(Keys.Down))
                translation -= transform.World.Forward * moveSpeed * gameTime.ElapsedGameTime.Milliseconds;

            if (Input.Keys.IsPressed(Keys.A) || Input.Keys.IsPressed(Keys.Left))
                translation += transform.World.Left * strafeSpeed * gameTime.ElapsedGameTime.Milliseconds;
            else if (Input.Keys.IsPressed(Keys.D) || Input.Keys.IsPressed(Keys.Right))
                translation += transform.World.Right * strafeSpeed * gameTime.ElapsedGameTime.Milliseconds;

           if (isGrounded)
                translation.Y = 0;

            //actually apply the movement
            transform.Translate(translation);
        }

        protected virtual void HandleMouseInput(GameTime gameTime)
        {
            
            rotation = Vector3.Zero;
            var delta = Input.Mouse.Delta;

            if (delta.Length() != 0)
            {
                //Q - where are X and Y reversed?
                rotation.Y -= delta.X * rotationSpeed.X * gameTime.ElapsedGameTime.Milliseconds;
                rotation.X -= delta.Y * rotationSpeed.Y * gameTime.ElapsedGameTime.Milliseconds;

                rotation.X = rotation.X < -1.5 ? -1.50f : rotation.X;
                rotation.X = rotation.X > 1.5 ? 1.50f : rotation.X;



                transform.SetRotation(rotation);
            }
        }

        #endregion Actions - Update, Input

        #region Actions - Gamepad (Unused)

        protected virtual void HandleGamepadInput(GameTime gameTime)
        {
        }

        #endregion Actions - Gamepad (Unused)
    }
}