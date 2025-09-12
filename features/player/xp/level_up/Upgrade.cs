using Godot;
using System;
public enum Rarity {
	Common,
	Rare,
	Legendary
}
public enum Stat
{
	MaxHealth,
	MovementSpeed,
	XpGain,
	CooldownReduction,
	LifeSteal,
	CriticalChance,
	CriticalDamage,
	Armor,
	Lucky,
	GeneralDamage,
	MagicSphereDamage,
	ArcaneWaveDamage,
	MortarDamage
}
[GlobalClass]
public partial class Upgrade : Resource
{
	[Export] public string Name {get;set;}
	[Export(PropertyHint.MultilineText)] public string Description {get;set;}
	[Export] public Rarity Rarity {get;set;}
	[Export] public Stat StatToUpgrade {get; set;}
	[Export] public float Value {get; set;}
}
