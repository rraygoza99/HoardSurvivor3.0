using Godot;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Features.Player.Characters.Types;
using System;

public partial class PlayerManager : Node
{
    [Export]
    private PackedScene playerScene;
    private Character currentCharacter;
    private Node3D playerNode;

    public static PlayerManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            GetTree().Root.CallDeferred("add_child", this);
        }
        else
        {
            QueueFree();
        }
    }

    public void CreateCharacter(string characterType)
    {
        switch (characterType)
        {
            case "Wizgod":
                currentCharacter = new Wizgod();
                break;
            default:
                GD.PrintErr($"Unknown character type: {characterType}");
                break;
        }
        playerNode = playerScene.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(playerNode);
        playerNode.Position = new Vector3(0, 1, 0);
        
    }
}
