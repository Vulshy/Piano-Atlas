# Piano-Atlas
Free visual piano theory app for students, teachers and self-taught musicians.
Piano Atlas for Windows
Version 1.0.0
Created by Vulshy

WHAT THIS APP IS
Piano Atlas is a clean piano reference app for learning basic scales, chords,
arpeggios, chord progressions, fingerings, and the circle of fifths.

It is made for Windows 10 and Windows 11.


HOW TO INSTALL
1. Unzip the PianoAtlas-Windows-Gumroad.zip file.
2. Double-click installer.exe.
3. Windows may show a SmartScreen warning because the app is not code-signed yet.
   Choose "More info" and then "Run anyway" if you trust this download.
4. The installer creates:
   - a Start Menu shortcut
   - a Desktop shortcut when Windows allows it
   - an uninstall entry in Windows Apps & Features

The app installs per user into:
%LOCALAPPDATA%\Programs\Piano Atlas

This means admin rights are usually not required.


HOW TO RUN WITHOUT INSTALLING
You can also double-click run.exe from this folder.

The app will open directly without installing anything. Keep run.exe next to the
app folder, runtime folder, and included DLL files. For the cleanest buyer
experience, installer.exe is still recommended.


HOW THE APP WORKS
Use the left panel to choose:
   - mode
   - root note
   - scale/chord/arpeggio/circle item
   - favorites

Use the Piano section to play the selected notes or pattern.

Use the fullscreen icon in the top-right controls if you want to enter or leave
fullscreen manually.

In Scales mode, the Chord progressions section can generate fitting progressions
for Classical, Jazz, Pop, and Blues styles.


FAVORITES AND SAVED DATA
Favorites and saved chord progressions are saved automatically by the app.

Saved user data is stored in this Windows user-data folder:
%LOCALAPPDATA%\Piano Atlas\UserData

This folder is separate from the install folder so saved favorites can survive
normal app updates.


HOW TO UNINSTALL
Use one of these methods:
   - Windows Settings > Apps > Installed apps > Piano Atlas > Uninstall
   - or run uninstaller.exe from:
     %LOCALAPPDATA%\Programs\Piano Atlas

During uninstall, you can choose whether to remove saved favorites/user data.


TROUBLESHOOTING
If the app does not open:
   - Make sure you are on Windows 10 or Windows 11.
   - Make sure Microsoft Edge WebView2 Runtime is available. It is included on
     modern Windows 10/11 systems and does not require developer tools.
   - Try running installer.exe again.
   - If you are running without installing, keep run.exe next to the app folder.

If favorites do not save:
   - Check that Windows allows the app to use:
     %LOCALAPPDATA%\Piano Atlas\UserData
   - Do not run the app from a temporary ZIP preview. Extract the ZIP first.

If Windows blocks the app:
   - The app is unsigned, so SmartScreen may warn you.
   - This is normal for small direct-download apps before code signing.


NOTES FOR GUMROAD BUYERS
No Node.js, npm, Python, developer tools, or command-line knowledge are required.
The app does not use localhost or development mode. The launcher is a native
Piano Atlas window, so the taskbar and shortcuts use the Piano Atlas logo.
