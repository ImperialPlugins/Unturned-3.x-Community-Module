# Contributing

1. Fork the repo and clone with `git clone --recursive https://github.com/YOURUSERNAME/Unturned-3.x-Community-Module`. You can of course use other methods to clone it, but the important step is to make sure you initialize and update the Harmony git submodule.
2. Create a new branch and name it `feature-xxxx`, e.g. `git checkout -b feature-sharks`
3. Copy the following files from `Unturned_Data/Managed/` to `unturned-lib`: `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll` and `UnityEngine.dll`
4. Make your modifications.
5. Build as Release and copy the contents of `Community-Module/bin/Release` to your Unturned/Modules/Community directory. Start Unturned without Battleye to test it.
6. Commit the changes and push them to your repo.
7. Create a pull request from your branch to the `mod` branch of the official repo.
