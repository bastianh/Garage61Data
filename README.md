### SimHub Plugin for Garage61 Data

This plugin allows SimHub to load best lap times from you, people you follow and your team members from **Garage61**.

⚠️ Warning: This is an unstable test version of the plugin. Use it at your own risk, as it may contain bugs or
incomplete features. It is intended for testing and feedback purposes only.

---

### Example Dashboard

![dashboard_example.png](docs/dashboard_example.png)

### Features

- **Team leaderboard**: Fetch fastest laps from people you follow or your team when you enter the game for the current
  lap / car

Properties set in SimHub:

| Property                | Datatype        | Description                                   |
|-------------------------|-----------------|-----------------------------------------------|
| LapCount                | number          | Number of available Laps (currently up to 16) |
| Lap.[1-16].FirstName    | string / null   | First name of the driver                      |
| Lap.[1-16].LastName     | string / null   | Last name of the driver                       |
| Lap.[1-16].DriverRating | number / null   | Rating of the driver at time of record        |
| Lap.[1-16].StartTime    | datetime / null | date of event when the lap was recorded       |
| Lap.[1-16].LapTime      | time  / null    | laptime of the recorded lap                   |

⚠️ Warning: Currently there are some deprecated properties starting with an additional `Garage61Data.`. Those will be
removed in a future version.

---

### Requirements

- **SimHub**: Download the latest version from [SimHub Official Website](https://www.simhubdash.com).
- **Garage61 Account**: Visit [Garage61](https://garage61.net) to sign up and access your data.

---

### Installation

1. Download the plugin from the [Releases](https://github.com/bastianh/Garage61Data/releases) page.
2. Place the plugin dll file in the SimHub folder.
3. Start SimHub and enable the Garage61 plugin in the Plugins menu.
4. Login to Garage61 on the plugin settings page

### When something does not work

- please check the "System Log" menu in SimHub and look for lines starting with `Garage61Data:`