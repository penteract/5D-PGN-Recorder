# 5D Chess With Multiverse Time Travel Notation Recorder

An **unofficial** program based on https://github.com/GHXX/FiveDChessDataInterface for saving notation from games of [5D Chess With Multiverse Time Travel](https://store.steampowered.com/app/1349230/5D_Chess_With_Multiverse_Time_Travel/).

## Usage
Start this program while 5D chess is running (or vice versa). After you finish a game with at least 4 moves, a file with the name `5dpgn<date>_<time>.txt` will be saved in the folder you ran the program from. This contains the move information, but doesn't always contain the name of the variant.
It will play the sound from Tick.wav when a player in a timed game has used 1/3 of their time since the start of their turn or the previous tick.
Note: The files produced contain your timezone (as an offset from UTC), so if you consider that information sensitve, remove that field or convert it to UTC (and don't forget to change the date if necessary) before sharing. It may also be possible to infer your timezone from the filename unless that is also changed.

## Disclaimer
While it should be stable for the most part, this program may still cause crashes/desyncs or other unexpected/unwanted behaviour, hence why the developers of this project cannot be held liable for any damage caused.

## Plans
Config file (name, which tags to include, sound file, how and whether to present time, always use white's perspective)
Automatic variant recognition (currently detects "Standard - Turn Zero", but I'm not sure it's always accurate when it claims that)
