using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineDraftCatalog")]
public sealed class OnlineDraftCatalogEditorTests
{
    [Test] public void CatalogAgreesWithExistingLoader([Values(false,true)]bool goalkeeper)
    {
        var root=new GameObject("Draft catalog verification");
        try
        {
            var manager=root.AddComponent<DraftManager>();
            typeof(DraftManager).GetMethod(goalkeeper?"LoadGKFromCSV":"LoadPlayersFromCSV",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{goalkeeper?"goalkeepers":"outfield_players"});
            var catalog=OnlineDraftCatalog.Load().Where(c=>c.goalkeeper==goalkeeper).ToArray();
            if(goalkeeper)
            {
                var expected=manager.allGks.Select(c=>string.Join("|",c.Name,c.Country,c.Type,c.SquadNumber,c.Pace,c.Dribbling,c.Aerial,c.HighPass,c.Resilience,c.Saving,c.Handling));
                Assert.That(catalog.Select(c=>string.Join("|",c.name,c.country,c.type,c.squadNumber,c.pace,c.dribbling,c.aerial,c.highPass,c.resilience,c.saving,c.handling)),Is.EquivalentTo(expected));
            }
            else
            {
                var expected=manager.allPlayers.Select(c=>string.Join("|",c.Name,c.Country,c.Type,c.SquadNumber,c.Pace,c.Dribbling,c.Heading,c.HighPass,c.Resilience,c.Shooting,c.Tackling));
                Assert.That(catalog.Select(c=>string.Join("|",c.name,c.country,c.type,c.squadNumber,c.pace,c.dribbling,c.heading,c.highPass,c.resilience,c.shooting,c.tackling)),Is.EquivalentTo(expected));
            }
        }
        finally{Object.DestroyImmediate(root);}
    }
}
