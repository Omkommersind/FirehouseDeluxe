using FirehouseDeluxe.Editor;
using FirehouseDeluxe.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FirehouseDeluxe.Tests
{
    public sealed class DrivingTests
    {
        private GameObject truck;
        private Rigidbody2D body;
        private TopDownVehicle vehicle;
        private SimulationMode2D previousSimulationMode;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            previousSimulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            truck = new GameObject("Test truck");
            body = truck.AddComponent<Rigidbody2D>();
            truck.AddComponent<BoxCollider2D>().size = new Vector2(2f, 4f);
            vehicle = truck.AddComponent<TopDownVehicle>();
            // Edit-mode physics is advanced explicitly so long drives run without wall-clock waits.
            vehicle.SendMessage("Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(truck);
            Physics2D.simulationMode = previousSimulationMode;
        }

        private void Drive(float seconds, float throttle, float steering = 0f, bool handbrake = false)
        {
            vehicle.SetDriveInput(throttle, steering, handbrake);
            for (int i = 0; i < Mathf.CeilToInt(seconds / Time.fixedDeltaTime); i++)
            {
                vehicle.SendMessage("FixedUpdate");
                Physics2D.Simulate(Time.fixedDeltaTime);
            }
        }

        [Test]
        public void IdleSteeringDoesNotMoveOrSpinTruck()
        {
            Drive(3f, 0f, 1f);
            Assert.That(body.position.magnitude, Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation)), Is.LessThan(0.001f));
        }

        [Test]
        public void AcceleratesGraduallyAndRespectsForwardSpeedLimit()
        {
            Drive(0.5f, 1f);
            Assert.That(body.linearVelocity.y, Is.InRange(1f, 3f));
            Drive(15f, 1f);
            Assert.That(body.linearVelocity.y, Is.InRange(17f, 18.01f));
            Assert.That(Mathf.Abs(body.position.x), Is.LessThan(0.001f));
        }

        [Test]
        public void CoastingLosesSpeedAndEventuallyStops()
        {
            Drive(3f, 1f);
            float speed = vehicle.SpeedKph;
            Drive(1f, 0f);
            Assert.That(vehicle.SpeedKph, Is.LessThan(speed));
            Drive(40f, 0f);
            Assert.That(vehicle.SpeedKph, Is.LessThan(0.001f));
        }

        [Test]
        public void OppositePedalBrakesBeforeReversing()
        {
            Drive(2f, 1f);
            Drive(0.2f, -1f);
            Assert.That(vehicle.ForwardSpeed, Is.GreaterThan(0f));
            Drive(3f, -1f);
            Assert.That(vehicle.ForwardSpeed, Is.InRange(-5.01f, -4.8f));
        }

        [Test]
        public void ReverseSpeedIsLowerThanForwardSpeed()
        {
            Drive(15f, -1f);
            Assert.That(body.linearVelocity.y, Is.InRange(-5.01f, -4.8f));
            Assert.That(body.position.y, Is.LessThan(-10f));
        }

        [TestCase(1f, 1f, -1f)]
        [TestCase(1f, -1f, 1f)]
        [TestCase(-1f, 1f, 1f)]
        [TestCase(-1f, -1f, -1f)]
        public void SteeringYawHasCorrectSignForForwardAndReverse(float throttle, float steering, float sign)
        {
            Drive(0.7f, throttle, steering);
            float angle = Mathf.DeltaAngle(0f, body.rotation);
            Assert.That(angle * sign, Is.GreaterThan(1f));
            Assert.That(Mathf.Abs(angle), Is.LessThan(45f));
        }

        [Test]
        public void SteeringProducesCurvedTravelAtArbitraryAngles()
        {
            Drive(1.2f, 1f, 0.6f);
            Assert.That(body.position.x, Is.GreaterThan(0.1f));
            Assert.That(body.position.y, Is.GreaterThan(1f));
            float angle = Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation));
            Assert.That(angle, Is.InRange(5f, 40f));
            Assert.That(Mathf.Abs(angle / 45f - Mathf.Round(angle / 45f)), Is.GreaterThan(0.05f));
        }

        [Test]
        public void GripRemovesSidewaysSliding()
        {
            body.linearVelocity = new Vector2(8f, 8f);
            Drive(0.5f, 0f);
            Assert.That(Mathf.Abs(body.linearVelocity.x), Is.LessThan(0.1f));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(6f));
        }

        [Test]
        public void HandbrakeStopsEngineAndReducesLateralGrip()
        {
            body.linearVelocity = new Vector2(8f, 8f);
            Drive(0.2f, 1f, 0f, true);
            Assert.That(body.linearVelocity.y, Is.LessThan(6f));
            Assert.That(body.linearVelocity.x, Is.GreaterThan(4f));
            Drive(2f, 1f, 0f, true);
            Assert.That(vehicle.SpeedKph, Is.LessThan(1f));
        }

        [Test]
        public void ResetRestoresSpawnAndClearsMomentumAndControls()
        {
            Drive(2f, 1f, 1f);
            vehicle.ResetToSpawn();
            Drive(0.1f, 0f);
            Assert.That(body.position.magnitude, Is.LessThan(0.001f));
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(body.rotation), Is.LessThan(0.001f));
        }

        [Test]
        public void DynamicTruckCannotDriveThroughAStaticWall()
        {
            var wall = new GameObject("Wall");
            try
            {
                wall.transform.position = new Vector3(0f, 12f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Physics2D.SyncTransforms();
                Drive(8f, 1f);
                Assert.That(body.position.y, Is.InRange(9f, 9.6f));
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void KeyboardSupportsWasdArrowsAndReset()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var input = truck.AddComponent<VehicleKeyboardInput>();
            input.SendMessage("Awake");
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                InputSystem.Update();
                input.SendMessage("Update");
                StepWithExistingInput(0.5f);
                Assert.That(vehicle.ForwardSpeed, Is.GreaterThan(0f));
                Assert.That(Mathf.DeltaAngle(0f, body.rotation), Is.LessThan(0f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                InputSystem.Update();
                input.SendMessage("Update");
                Assert.That(body.position.magnitude, Is.LessThan(0.001f));
                Assert.That(vehicle.SpeedKph, Is.LessThan(0.001f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow, Key.RightArrow));
                InputSystem.Update();
                input.SendMessage("Update");
                StepWithExistingInput(0.5f);
                Assert.That(vehicle.ForwardSpeed, Is.LessThan(0f));
                Assert.That(Mathf.DeltaAngle(0f, body.rotation), Is.GreaterThan(0f));
                input.SendMessage("OnApplicationFocus", false);
                float before = vehicle.SpeedKph;
                StepWithExistingInput(0.1f);
                Assert.That(vehicle.SpeedKph, Is.LessThan(before));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private void StepWithExistingInput(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds / Time.fixedDeltaTime); i++)
            {
                vehicle.SendMessage("FixedUpdate");
                Physics2D.Simulate(Time.fixedDeltaTime);
            }
        }

        [Test]
        public void PlaygroundHasValidPrefabCameraAndVisibleSprite()
        {
            var scene = EditorSceneManager.OpenScene(RoadTilemapEditor.ScenePath);
            var motor = Object.FindFirstObjectByType<TopDownVehicle>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(motor), Is.True);
            var sprite = motor.GetComponentInChildren<SpriteRenderer>();
            Assert.That(sprite.sprite, Is.Not.Null);
            Assert.That(sprite.sharedMaterial.shader.name,
                Is.EqualTo("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            Assert.That(motor.GetComponent<Rigidbody2D>().gravityScale, Is.Zero);
            Assert.That(Camera.main.orthographic, Is.True);
            Assert.That(Camera.main.backgroundColor, Is.EqualTo(Color.black));
            Assert.That(Camera.main.GetComponent<VehicleFollowCamera>().Target, Is.EqualTo(motor.transform));
            Assert.That(scene.isLoaded, Is.True);
        }
    }
}
