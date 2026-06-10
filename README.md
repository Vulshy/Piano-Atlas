# Piano Atlas

Piano Atlas is a piano reference app for scales, chords, arpeggios, the circle of fifths, chord progressions, favorites, light/dark mode, and interactive piano playback.

## Main App Code

- `index.html` is the complete browser version of the app.
- `app/PianoAtlas.html` is the same app file, named for the Windows desktop package.
- `app/PianoAtlas.ico` is the app icon used by the Windows build.

The app is currently built as a single HTML file containing the HTML, CSS, and JavaScript together. You can open `index.html` directly in a browser to view the source version.

## Windows Source

The `windows-source/` folder contains the source files that were used for the Windows desktop launcher, installer, and uninstaller.

These are included for transparency, but this folder is not a full build system by itself. The finished Gumroad package is still kept separately in `outputs/PianoAtlas-Windows-Gumroad/`.

## Data Saving

In the browser version, favorites are saved with browser local storage.

In the Windows app, saved user data is stored through the WebView user data folder under the user's Windows account.

## License

See `LICENSE.txt`.
