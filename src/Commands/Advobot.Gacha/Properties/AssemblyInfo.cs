﻿using Advobot.CommandMarking;
using Advobot.Gacha;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCompany("Advorange")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCopyright("Copyright © 2018")]
[assembly: AssemblyDescription("Some shitty attempt at a gacha game in Discord.")]
[assembly: AssemblyProduct("Advobot")]
[assembly: AssemblyTitle("Advobot.Gacha")]
[assembly: NeutralResourcesLanguage("en")]

// Indicates the assembly has commands in it for the bot to use
[assembly: CommandAssembly(InstantiatorType = typeof(GachaInstantiation))]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c2fce325-6db2-46bc-a4e8-5771f7e49c9f")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion(Advobot.Constants.BOT_VERSION)]
[assembly: AssemblyFileVersion(Advobot.Constants.BOT_VERSION)]
[assembly: AssemblyInformationalVersion(Advobot.Constants.BOT_VERSION)]