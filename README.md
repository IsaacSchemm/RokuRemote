# RokuRemote

This is a .NET WinForms app to control Roku devices on the local network.

The mouse can be used to control the virtual remote on the screen, or you can perform certain functions with the keyboard:

* Back (Esc)
* Home
* Up
* Down
* Left
* Right
* OK (Enter)
* Play/Pause
* Channel Up (Page Up)
* Channel Down (Page Down)

The keyboard can also be used for text input (printable characters and the Backspace button).

Additional functions are available if keyboard control is turned off:

* **Media**: Copy a media URL (MP4, HLS, etc) from the Internet into this box and the Roku will try to play it.
    * If the domain is `youtube.com` or `youtu.be`, the Remote application will first check the URL to find its YouTube video ID (if any); if the ID is found, the video will be launched inside the YouTube app on the Roku.
* **Search**: Enter a search term into this box and it will be sent to the Roku, which will display matching results.
