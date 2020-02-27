//Planned Features:
// - Color Coded Main Screen
// - Sorter Manager
// - Tag Manager
// - Mode Manager
// - Event Manager
// - Rotor & Piston Manager
// - Usefull Screens

//Fixable Issues:

//Grid Manager by Kezeslabas V3.0
// These are a couple of usefull scripts that I made for my personal use that I grouped together for easier access.

//Script List
// - Simple Airlock Manager
// - Sorter Manager [WIP]
// - Tag Manager [WIP]
// - Mode Manager [WIP]
// - Event Manager [WIP]
// - Rotor & Piston Manager [WIP]
// - Usefull Screens [WIP]

//Scirpt Notes:

// Simple Airlock Manager ----------
//  Setup:
//  - Build an Airlock out of Doors
//  - Find a name for your Airlock, then add that name to the names of the Doors
//  - Find names for the different sides of the Airlock, then add the right side names to the right Doors

//  How to use:
//  - Use a Button, or Sensor to run this Programmable Block
//  - Use an argument, like: "Airlock;/Airlock Name/;/Side Name/ to request on open to that side
//      - Example: Airlock;/A-01/;/Outside/

//  Notes:
//  - The script is capable of handling any number of Airlocks with any number of Doors and Sides, at the same time.
//  - If the script gets an opening request for a specific Airlock's specific Side,
//    then it closes every Door in that Airlock that isn't the requested side's, then
//    if all of those Doors are closed, it opens the requested side's Doors.

// Sorter Manager ----------
//  Setup:
//  - Build Conveyor Sorters
//  - Find a group name for the Sorters you want to handle the same, then add that group name to their names.
//  - Identify the Sorters that are going In to an area, and Out, then add the /In/ or /Out/ tag to their names.
//      - The /In/ Sorters will be set to WhiteList and Drain Off
//      - The /Out/ Sorters will be set to Blacklist and Drain On

//  How to use:
//  - Run this Programmable Block with an argument, like "Sorter;/Group Name/;<Type>
//  - The <Type> can be:
//      - "RareOre": Gold, Magnesium, Platinum, Silver, Uranium
//      - "CommonOre": Cobalt, Iron, Nickel, Silicon, Stone
//      - "Junk": Stone and Gravel
//      - "Ingots": All Ingots
//      - "Components": All Components
//      - "Production": All Ores and Ingots


//Generic Data
bool ImSet=false;
string MyTag="";
sbyte ImRunning=0;
bool AirlockIsRunning=false;

//To Reduce Allocation
int _i,_k;
string _result;
string[] _data;

MyIni _Ini = new MyIni();
MyIniParseResult _IniResult;

//IMyTerminalBlock _Block;
//List<IMyTerminalBlock> _Blocks = new List<IMyTerminalBlock>();

ScreenMessage MyMessage = new ScreenMessage();
MyArgumentDecoder ArgumentDecoder = new MyArgumentDecoder();

IMyDoor _Door;
List<IMyDoor> _Doors = new List<IMyDoor>();

MyAirlock _Airlock = new MyAirlock();
List<MyAirlock> Airlocks = new List<MyAirlock>();

public class ScreenMessage
{
    public string Message;
    public string Script;
    public ulong RunCount;

    string LastMessage;
    string LastMessage2;
    string Report;
    string Header;
    string SubHeader;

    IMyTextSurface MyScreen;

    public ScreenMessage()
    {
        Message="";
        Script="";
        RunCount=0;

        LastMessage="";
        LastMessage2="";
        Report="";
        Header="";
        SubHeader="";
    }
    public void New(IMyTextSurface _surface)
    {
        Message="Grid Manager by Kezeslabas";
        Script="";
        RunCount=0;

        LastMessage="";
        LastMessage2="";
        Report="";
        Header="";
        SubHeader="";

        MyScreen=_surface;
        MyScreen.ContentType=ContentType.TEXT_AND_IMAGE;
    }
    public void ConstructMsg()
    {
        Message="#"+RunCount+" --- "+Script+"\n";
        Message+=Report;
    }
    public void AddReport(string _report)
    {
        Report+=_report+"\n";
    }
    public void AddSubHeader(string _subHeader)
    {
        SubHeader+=_subHeader+"\n";
    }
    public void Update()
    {
        MyScreen.WriteText(Header+SubHeader+Message+"\n"+LastMessage+"\n"+LastMessage2);
        SubHeader="";
    }
    public void Continue()
    {
        LastMessage2=LastMessage;
        LastMessage=Message;
        Report="";
        RunCount++;
    }
    public void SetHeader(string _header)
    {
        Header="[Grid]: "+_header+"\n";
    }
}

public class MyArgumentDecoder
{
    public ScriptType Script;
    public string Command;
    public string[] ArgData;
    public string Argument;

    string _CurrentString;

    public MyArgumentDecoder()
    {
        Script=ScriptType.NONE;
        Command="";
        Argument="";
    }
    public bool GetArgument(string _arg)
    {
        Argument=_arg;
        _CurrentString= _arg.Split(';')[0];
        _CurrentString=_CurrentString.ToLower();
        ArgData=_arg.Split(';');
        switch(_CurrentString)
        {
            case "airlock":
            {
                Script=ScriptType.AIRLOCK;
                return true;
            }
            case "tag":
            {
                Script=ScriptType.TAG;
                return true;
            }
            case "rpmanager":
            {
                Script=ScriptType.RPMANAGER;
                return true;
            }
            case "sorter":
            {
                Script=ScriptType.SORTER;
                return true;
            }
            case "set":
            {
                Script=ScriptType.SET;
                return true;
            }
            default:
            {
                return false;
            }
        }
    }
}

public enum ScriptType
{
    TAG,
    AIRLOCK,
    RPMANAGER,
    SORTER,
    SET,
    NONE
}

//Start of Program

public Program()
{
    MyMessage.New(Me.GetSurface(0));

    if(!GetConfig())ResetConfig();

    LoadData();

    if(ImSet)SetGrid(true);
    else MyMessage.AddSubHeader("[System]: Welcome!\nPlease name your grid!\n( set;/Grid Name/ )");

    MyMessage.Update();
}

public void Save()
{
    SaveData();
}

public void Main(string argument, UpdateType updateSource)
{
    if((updateSource & UpdateType.Update10)!=0)
    {
        if(ImRunning>0)
        {
            if(ImSet)
            {
                if(AirlockIsRunning)UpdateAirlocks();
            }
            else
            {
                StopScript();
            }
        }
        else PauseScript();
    }
    else
    {
        MyMessage.Continue();
        if(ImSet)
        {
            if(ArgumentDecoder.GetArgument(argument))
            {
                if(ArgumentDecoder.Script==ScriptType.AIRLOCK)
                {
                    SetScript("Airlock");
                    RunAirlockScript();
                }
                else if(ArgumentDecoder.Script==ScriptType.TAG)
                {
                    SetScript("Tag");
                    RunTagScript();
                }
                else if(ArgumentDecoder.Script==ScriptType.SORTER)
                {
                    SetScript("Sorter");
                    RunSorterScript();
                }
                else if(ArgumentDecoder.Script==ScriptType.RPMANAGER)
                {
                    SetScript("RP Manager");
                    RunRpManagerScript();
                }
                else if(ArgumentDecoder.Script==ScriptType.SET)
                {
                    SetScript("System");
                    SetGrid();
                }
            }
            else
            {
                SetScript("System");
                MyMessage.AddReport("[Warning]: Incorrect Argument!");
            }
        }
        else
        {
            if(ArgumentDecoder.GetArgument(argument))
            {
                SetScript("System");
                if(ArgumentDecoder.Script==ScriptType.SET)
                {
                    SetGrid();
                }
                else
                {
                    MyMessage.AddReport("[Warning]: System must be named first!\n( set;/Grid Name/ )");
                }
            }
            else
            {
                SetScript("System");
                MyMessage.AddReport("[Warning]: System must be named first!\n( set;/Grid Name/ )");
            }
        }
        MyMessage.ConstructMsg();
    }
    MyMessage.Update();
}

//End of Program

//Airlock Script

public class MyAirlock
{
    public string Name;
    public string Side;

    public bool Remove;

    public List<IMyDoor> OpenDoors = new List<IMyDoor>();
    public List<IMyDoor> CloseDoors = new List<IMyDoor>();

    bool ICanOpen;

    IMyDoor _CurrentDoor;
    bool _Indicator; 
    string _Report;
    int _i;

    public MyAirlock(string _name="",string _side="")
    {
        Name=_name;
        Side=_side;
        Remove=false;

        OpenDoors = new List<IMyDoor>();
        CloseDoors = new List<IMyDoor>();

        ICanOpen=false;

        _Indicator=false;
        _Report="";

        _i=0;
    }
    public void New(string _name, string _side)
    {
        Name=_name;
        Side=_side;
        Remove=false;

        ICanOpen=false;

        OpenDoors.Clear();
        CloseDoors.Clear();
    }
    public void Update()
    {
        
        if(!ICanOpen)
        {
            ICanOpen=true;
            for(_i=0;_i<CloseDoors.Count;_i++)
            {
                _CurrentDoor=CloseDoors[_i];
                if(ImHere())
                {
                    if(_CurrentDoor.Status!=DoorStatus.Closed)
                    {
                        if(_CurrentDoor.Status!=DoorStatus.Closing)
                        {
                            _CurrentDoor.CloseDoor();
                        }
                        if(!_CurrentDoor.Enabled)
                        {
                            _CurrentDoor.Enabled=true;
                        }
                        ICanOpen=false;
                    }
                    else
                    {
                        _CurrentDoor.Enabled=false;
                    }
                }
            }
        }
        if(ICanOpen)
        {

            Remove=true;
            for(_i=0;_i<OpenDoors.Count;_i++)
            {
                _CurrentDoor=OpenDoors[_i];
                if(ImHere())
                {
                    if(_CurrentDoor.Status!=DoorStatus.Open)
                    {
                        if(_CurrentDoor.Status!=DoorStatus.Opening)
                        {
                            _CurrentDoor.OpenDoor();
                        }
                        if(!_CurrentDoor.Enabled)
                        {
                            _CurrentDoor.Enabled=true;
                        }
                        Remove=false;
                    }
                }
            }
        }
    }
    public string ConstructReport()
    {
        _Report="";
        if(_Indicator)_Report+="[-/-/-/] ";
        else _Report+="[/-/-/-] ";
        _Report+=Name+" | "+Side;
        _Indicator=!_Indicator;
        return _Report;
    }
    public bool ImHere()
    {
        if(_CurrentDoor==null || _CurrentDoor.CubeGrid.GetCubeBlock(_CurrentDoor.Position)==null)return false;
        else return true;
    }
}

public void RunAirlockScript()
{
    if(ArgumentDecoder.ArgData.Length>=3)
    {
        _Airlock = new MyAirlock(ArgumentDecoder.ArgData[1],ArgumentDecoder.ArgData[2]);

        for(_i=0;_i<Airlocks.Count;_i++)
        {
            if(Airlocks[_i].Name==_Airlock.Name)
            {
                Airlocks.RemoveAt(_i);
                break;
            }
        }

        _Doors.Clear();
        GridTerminalSystem.GetBlocksOfType<IMyDoor>(_Doors);

        for(_i=0;_i<_Doors.Count;_i++)
        {
            _Door=_Doors[_i];
            if(_Door.CustomName.Contains(_Airlock.Name) && _Door.CustomName.Contains(MyTag))
            {
                if(_Door.CustomName.Contains(_Airlock.Side))_Airlock.OpenDoors.Add(_Door);
                else _Airlock.CloseDoors.Add(_Door);
            }
        }

        if(_Airlock.OpenDoors.Count>0)
        {
            if(_Airlock.CloseDoors.Count>0)
            {
                Airlocks.Add(_Airlock);
                MyMessage.AddReport("[Airlock]: "+_Airlock.Name+" | "+_Airlock.Side
                                    +"\n[Open]: "+_Airlock.OpenDoors.Count+" | [Close]: "+_Airlock.CloseDoors.Count);
                
                StartScript();
                AirlockIsRunning=true;
                UpdateAirlocks();
            }
            
        }
        else if(_Airlock.CloseDoors.Count==0)
        {
            MyMessage.AddReport("[Warning]: Airlock not found!");
        }
    }
}

public void UpdateAirlocks()
{
    for(_i=0;_i<Airlocks.Count;_i++)
    {
        _Airlock=Airlocks[_i];
        _Airlock.Update();
        if(!_Airlock.Remove)MyMessage.AddSubHeader(_Airlock.ConstructReport());
    }
    for(_i=0;_i<Airlocks.Count;_i++)
    {
        if(Airlocks[_i].Remove)
        {
            Airlocks.RemoveAt(_i);
            _i=0;
        }
    }
    if(Airlocks.Count==0)
    {
        AirlockIsRunning=false;
        PauseScript();
    }
}

public void ReBuildAirlocks()
{
    _Doors.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(_Doors);

    for(_i=0;_i<Airlocks.Count;_i++)
    {
        _Airlock=Airlocks[_i];
        for(_k=0;_k<_Doors.Count;_k++)
        {
            _Door=_Doors[_k];
            if(_Door.CustomName.Contains(_Airlock.Name) && _Door.CustomName.Contains(MyTag))
            {
                if(_Door.CustomName.Contains(_Airlock.Side))
                {
                    Airlocks[_i].OpenDoors.Add(_Door);
                }
                else
                {
                    Airlocks[_i].CloseDoors.Add(_Door);
                }
            }
        }
    }
}

//Sorter Script
public void RunSorterScript()
{

}

//Tag Script
public void RunTagScript()
{

}

//Rotor Piston Script
public void RunRpManagerScript()
{

}

//Generic
public void SetGrid(bool auto=false)
{
    if(auto)
    {
        MyMessage.SetHeader(MyTag);
    }
    else if(ArgumentDecoder.ArgData.Length>=2)
    {
        MyTag=ArgumentDecoder.ArgData[1];
        ImSet=true;
        MyMessage.SetHeader(MyTag);
        MyMessage.AddReport("[System]: Naming Completed!");
        SaveData();
    }
    else
    {
        MyMessage.AddReport("[Warning]: Incorrect Argument!");
    }
}

public void StartScript()
{
    if(ImRunning==0)
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        MyMessage.AddReport("[System]: Running...");
    }
    ImRunning++;
}

public void PauseScript()
{
    if(ImRunning>0)
    {
        if(ImRunning==1)
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            MyMessage.AddReport("[System]: Paused");
        }
        ImRunning--;
    }
    else ImRunning=0;
}

public void StopScript()
{
    ImRunning=0;
    Runtime.UpdateFrequency = UpdateFrequency.None;
    MyMessage.AddReport("[System]: Stopped");
}

public bool ComponentIsReady(IMyTerminalBlock _comp)
{
   if(_comp==null || _comp.CubeGrid.GetCubeBlock(_comp.Position)==null)return false;
   else return true;
}

public void SetScript(string _scriptName)
{
    MyMessage.Script=_scriptName+" | "+ArgumentDecoder.Argument;
}

//Configuration and Storage
public void SaveData()
{
    _Ini.Clear();

    _Ini.Set("Generic","ImSet",ImSet);
    if(ImSet)
    {
        _Ini.Set("Generic","MyTag",MyTag);
        _Ini.Set("MyMessage","RunCount",MyMessage.RunCount);
        _Ini.Set("Generic","ImRunning",ImRunning);

        _Ini.Set("Generic","AirlockIsRunning",AirlockIsRunning);
        if(AirlockIsRunning)
        {
            _result="";
            for(_i=0;_i<Airlocks.Count;_i++)
            {
                _Airlock=Airlocks[_i];
                _result+=_Airlock.Name+"\n"+_Airlock.Side+"\n";
            }
            _Ini.Set("Airlock","AirlockValues",_result);
        }
    }
    Storage=_Ini.ToString();

    MyMessage.AddReport("[System]: Data Saved");
}

public void LoadData(bool first=true)
{
    if(first)
    {
        if(Storage=="" || !_Ini.TryParse(Storage, out _IniResult))MyMessage.AddReport("[System]: Load Failed!");
    }
    
    if(_IniResult.IsDefined && _IniResult.Success)
    {
        if(!_Ini.Get("Generic","ImSet").TryGetBoolean(out ImSet))MyMessage.AddReport("[Load Error]: ImSet");
        else if(ImSet)
        {
            if(!_Ini.Get("Generic","MyTag").TryGetString(out MyTag))MyMessage.AddReport("[Load Error]: MyTag");

            if(!_Ini.Get("MyMessage","RunCount").TryGetUInt64(out MyMessage.RunCount))MyMessage.AddReport("[Load Error]: MyMessage.RunCount");

            if(!_Ini.Get("Generic","ImRunning").TryGetSByte(out ImRunning))MyMessage.AddReport("[Load Error]: ImRunning");
            else if(ImRunning>0)Runtime.UpdateFrequency = UpdateFrequency.Update10;
            else Runtime.UpdateFrequency = UpdateFrequency.None;

            if(!_Ini.Get("Generic","AirlockIsRunning").TryGetBoolean(out AirlockIsRunning))MyMessage.AddReport("[Load Error]: AirlockIsRunning");
            else if(AirlockIsRunning)
            {
                if(!_Ini.Get("Airlock","AirlockValues").TryGetString(out _result))MyMessage.AddReport("[Load Error]: AirlockValues");
                else
                {
                    if(_result!="")
                    {
                        Airlocks.Clear();
                        _data = _result.Split('\n');
                        for(_i=1;_i<_data.Length;_i=_i+2)
                        {
                            Airlocks.Add(new MyAirlock(_data[_i-1],_data[_i]));
                        }
                        ReBuildAirlocks();
                        StartScript();
                    }
                }
            }
        }
        MyMessage.AddReport("[System]: Data Loaded");
    }
}

public void ResetConfig()
{
    //Reset the Config

    MyMessage.AddReport("[System]: Configuration Reset");
}

public void SetConfig()
{
    Me.CustomData="";
}

public bool GetConfig(bool optionalOnly=false)
{
    if(Me.CustomData!="" && _Ini.TryParse(Me.CustomData, out _IniResult))
    {


        MyMessage.AddReport("[System]: Configuration loaded...");
        return true;
    }
    else
    {
        MyMessage.AddReport("[System]: Configuration failed!");
        return false;
    }
}