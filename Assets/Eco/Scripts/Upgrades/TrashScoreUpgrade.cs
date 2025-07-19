using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/TrashScoreUpgrade")]
public class TrashScoreUpgrade : Upgrade
{
    public TrashType trashType;
    public int ScoreForCurrentUpgrade = 1;

    protected override void Load()
    {
        base.Load();
        ScoreForCurrentUpgrade = 1;
    }

    protected override void ApplyUpgrade(int level)
    {
        ScoreForCurrentUpgrade = level * 2;
    }
}