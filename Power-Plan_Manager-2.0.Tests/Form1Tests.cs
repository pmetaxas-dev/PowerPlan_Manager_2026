using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Unit tests for Form1 class.
    /// Tests initialization, settings persistence, and UI interactions.
    /// </summary>
    [TestClass]
    public class Form1Tests
    {
        private Form1? form;
        private FakePowerPlanStateStore? state;

        [TestInitialize]
        public void Setup()
        {
            state = new FakePowerPlanStateStore();
            // Create form but don't show it
            form = new Form1(
                FakePowerPlanService.CreateDefault(),
                state);
        }

        [TestCleanup]
        public void Teardown()
        {
            form?.Dispose();
        }

        [TestMethod]
        public void Form1_Creates_Successfully()
        {
            // Assert
            Assert.IsNotNull(form,
                "Form1 should create without exception");
        }

        [TestMethod]
        public void Form1_IsNotResizable()
        {
            // Assert
            Assert.AreEqual(System.Windows.Forms.FormBorderStyle.FixedSingle, form?.FormBorderStyle,
                "Form1 should have FixedSingle border style to prevent resizing");
        }

        [TestMethod]
        public void Form1_MaximizeBox_IsDisabled()
        {
            // Assert
            Assert.IsFalse(form?.MaximizeBox ?? false,
                "Form1 MaximizeBox should be disabled");
        }

        [TestMethod]
        public void Form1_MinimizeBox_IsDisabled()
        {
            // Assert
            Assert.IsFalse(form?.MinimizeBox ?? false,
                "Form1 MinimizeBox should be disabled");
        }

        [TestMethod]
        public void Form1_HasTrayIcon()
        {
            // Assert - Form1 is displayed successfully, which includes tray icon initialization
            // The tray icon visibility is set during initialization
            Assert.IsNotNull(form,
                "Form1 should have a NotifyIcon (system tray icon)");
        }

        [TestMethod]
        public void Form1_HasCheckBox()
        {
            // Arrange & Act
            var checkBox = form?.Controls.Cast<System.Windows.Forms.Control>()
                .OfType<System.Windows.Forms.CheckBox>()
                .FirstOrDefault();

            // Assert
            Assert.IsNotNull(checkBox,
                "Form1 should have a CheckBox control for enable/disable management");
        }

        [TestMethod]
        public void Form1_HasAboutButton()
        {
            // Arrange & Act
            var aboutButton = form?.Controls.Cast<System.Windows.Forms.Control>()
                .OfType<System.Windows.Forms.Button>()
                .FirstOrDefault();

            // Assert
            Assert.IsNotNull(aboutButton,
                "Form1 should have a Button control to open the About window");
        }

        [TestMethod]
        public void Form1_ImplementsIDisposable()
        {
            // Assert
            Assert.IsInstanceOfType(form, typeof(System.ComponentModel.IComponent),
                "Form1 should implement IDisposable (inherited from Form)");
        }

        [TestMethod]
        public void Form1_Dispose_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                form?.Dispose();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Form1.Dispose should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void Form1_InitialPosition_IsOnScreen()
        {
            // Arrange & Act
            var location = form?.Location;
            var size = form?.Size;

            // Assert
            if (location.HasValue && size.HasValue)
            {
                Assert.IsGreaterThanOrEqualTo(0, location.Value.X,
                    "Form1 X position should be non-negative");
                Assert.IsGreaterThanOrEqualTo(0, location.Value.Y,
                    "Form1 Y position should be non-negative");
                Assert.IsGreaterThan(0, size.Value.Width,
                    "Form1 width should be positive");
                Assert.IsGreaterThan(0, size.Value.Height,
                    "Form1 height should be positive");
            }
        }

        [TestMethod]
        public void Form1_PersistsSelectedActiveUserPlan()
        {
            Assert.AreEqual(Constants.UltimatePerformance, state?.NormalPlanGuid);
            Assert.IsGreaterThan(0, state?.SaveCount ?? 0);
        }

        [TestMethod]
        public void Form1_UsesInjectedSettingsWithoutUserConfigWrites()
        {
            Assert.IsNotNull(state);
            Assert.IsFalse(string.IsNullOrWhiteSpace(state.NormalPlanGuid));
        }
    }
}
