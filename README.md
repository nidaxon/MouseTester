# Raw Input Mouse Polling Tester

A lightweight, standalone Windows utility to accurately measure your mouse's polling rate (Hz). 

Standard Windows mouse events are often throttled by the OS. This program uses the **Windows Raw Input API** to read directly from the hardware, making it highly accurate for testing high-performance gaming mice (1000Hz, 4000Hz, 8000Hz+).

## Features
* **Accurate Measurement:** Bypasses OS limitations for true hardware-level polling rates.
* **Live Stats:** Displays Current, Average, and Maximum Hz in real-time.
* **Lightweight:** Minimal CPU usage and a clean, dark-mode UI.

## How to Use
Just launch the program and move your mouse rapidly in continuous circles inside the program window to test your polling rate.

## License
This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.
