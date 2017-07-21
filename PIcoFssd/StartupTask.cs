using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

/***
 * 
 * Author: Demetrice Ledbetter
 * 
 * This was adapted from https://github.com/modmypi/PiModules/blob/master/code/python/upspico/picofssd/scripts/picofssd
 * and is very bare bones, but should help get anyone trying to pair the 
 * UPS PIco (http://www.pimodules.com/_pdf/_pico/UPS_PIco_BL_FSSD_V1.0.pdf) with a Raspberry Pi 3 using Windows IoT. 
 * Feel free to add on/improve/use as you see fit.
 * 
 * Provided without any warranty or guarantee. Author is not responsibie for anything going wrong :)
 * 
 */

namespace PIcoFssd
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private static GpioController gpioController = GpioController.GetDefault();
        private static int counter;

        // GPIO Pin Numbers
        private const int PICO_PULSE_PIN = 22;
        private const int PICO_CLOCK_PIN = 27;
        private const int BOUNCE_TIME = 30;

        // GPIO Pins
        private static GpioPin clockPin = gpioController.OpenPin(PICO_CLOCK_PIN);
        private static GpioPin pulsePin = gpioController.OpenPin(PICO_PULSE_PIN);

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            InitPico();

            while (true)
            {
                Task.Delay(5000);
            }

            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
        }

        private void InitPico()
        {
            // Set the DebounceTimeout to ignore noisey signals
            clockPin.DebounceTimeout = TimeSpan.FromMilliseconds(BOUNCE_TIME);
            pulsePin.DebounceTimeout = TimeSpan.FromMilliseconds(BOUNCE_TIME);

            clockPin.SetDriveMode(clockPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                ? GpioPinDriveMode.InputPullUp
                : GpioPinDriveMode.Input);

            pulsePin.Write(GpioPinValue.High);
            pulsePin.SetDriveMode(GpioPinDriveMode.Output);

            // Add listener
            clockPin.ValueChanged += Pin_ValueChanged;
        }

        // Button Listener
        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                // This test is here because the user *might* have another HAT plugged in or another circuit that produces a
                // falling-edge signal on another GPIO pin.
                if (sender != clockPin)
                {
                    return;
                }

                // We can get the state of a pin with GPIO.input even when it is currently configured as an output
                // Python: self.sqwave = not GPIO.input(PULSE_PIN)
                GpioPinValue initialValue = (pulsePin.Read() == GpioPinValue.High
                    ? GpioPinValue.Low
                    : GpioPinValue.High);

                // Set pulse pin low before changing it to input to look for shutdown signal
                // Python: GPIO.output(PULSE_PIN, False)
                pulsePin.Write(GpioPinValue.Low);

                // Python: GPIO.setup(PULSE_PIN, GPIO.IN, pull_up_down = GPIO.PUD_UP)
                pulsePin.SetDriveMode(clockPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                    ? GpioPinDriveMode.InputPullUp
                    : GpioPinDriveMode.Input);

                if (pulsePin.Read() == GpioPinValue.Low)
                {
                    counter = counter++;
                    Debug.WriteLine("Lost power, starting shutdown");
                    Task.Delay(new TimeSpan(0, 0, 2));

                    Debug.WriteLine("Shutting down...");

                    // Shutdown the device immediately
                    ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
                }
                else
                {
                    counter = 0;
                }

                pulsePin.SetDriveMode(GpioPinDriveMode.Output);
                pulsePin.Write(initialValue);
            }
        }
    }
}
