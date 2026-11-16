using System;
namespace CounterAttack.Online
{
    internal sealed class OnlineDraftDragSelection
    {
        internal readonly OnlineDraftView view;
        internal readonly OnlineDraftClient client;
        internal readonly long connection;
        internal readonly bool card,goalkeeper;
        internal readonly int index;
        internal readonly string id;
        internal OnlineDraftDragSelection(OnlineDraftView view,OnlineDraftClient client,long connection,bool card,int index,OnlineDraftCard player)
        {this.view=view;this.client=client;this.connection=connection;this.card=card;this.index=index;id=player.id;goalkeeper=player.goalkeeper;}
    }
    public sealed partial class OnlineDraftView
    {
        private OnlineDraftState rendered;
        private OnlineDraftDragSelection activeDrag;
        internal bool TryBeginDrag(bool card,OnlineTeam team,int index,out OnlineDraftDragSelection selection)
        {
            selection=null;var s=rendered;
            if(!online || activeDrag!=null || client==null || !client.Connected || client.Pending || s==null || s.phase is not ("pick" or "review"))return false;
            OnlineDraftCard player;
            if(card)
            {
                if(s.phase!="pick" || !s.repeatable && s.turn!=client.Team)return false;
                var visible=DisplayedCards(s);if(index<0 || index>=visible.Length)return false;player=visible[index];
            }
            else
            {
                var roster=client.Team==OnlineTeam.Home?s.home:s.away;
                if(team!=client.Team || index<1 || index>roster.Length || roster[index-1]==null)return false;player=roster[index-1];
            }
            selection=activeDrag=new OnlineDraftDragSelection(this,client,s.connectionRevision,card,index,player);selected=null;selectedSlot=0;Render();return true;
        }
        internal bool DragIsCurrent(OnlineDraftDragSelection selection)
        {
            var s=rendered;
            if(selection==null || !ReferenceEquals(activeDrag,selection) || selection.view!=this || !ReferenceEquals(selection.client,client)
                || client==null || !client.Connected || client.Pending || s==null || s.connectionRevision!=selection.connection || s.phase is not ("pick" or "review"))return false;
            if(selection.card)
            {
                if(s.phase!="pick" || !s.repeatable && s.turn!=client.Team)return false;
                var visible=DisplayedCards(s);return selection.index>=0 && selection.index<visible.Length && visible[selection.index].id==selection.id;
            }
            var roster=client.Team==OnlineTeam.Home?s.home:s.away;
            return selection.index>=1 && selection.index<=roster.Length && roster[selection.index-1]?.id==selection.id;
        }
        internal bool CompleteDrag(OnlineDraftDragSelection selection,OnlineTeam team,int slot)
        {
            bool current=DragIsCurrent(selection);if(ReferenceEquals(activeDrag,selection))activeDrag=null;
            if(!current || team!=client.Team){RefreshAfterDrag();return false;}
            var roster=client.Team==OnlineTeam.Home?rendered.home:rendered.away;
            if(slot<1 || slot>roster.Length || !selection.card && (slot==selection.index || GoalkeeperSlot(slot)!=GoalkeeperSlot(selection.index))
                || selection.card && !selection.goalkeeper && GoalkeeperSlot(slot)){RefreshAfterDrag();return false;}
            // Picks keep the authority's existing GK/occupied-slot placement policy.
            bool sent=selection.card?client.Request(OnlineDraftIntent.Pick,selection.id,slot)
                :client.Request(OnlineDraftIntent.Swap,null,selection.index,slot);
            if(!sent)RefreshAfterDrag();return sent;
        }
        internal void CancelDrag(OnlineDraftDragSelection selection)
        {if(!ReferenceEquals(activeDrag,selection))return;activeDrag=null;RefreshAfterDrag();}
        private static bool GoalkeeperSlot(int slot)=>slot is 1 or 12;
        private void RefreshAfterDrag()
        {if(isActiveAndEnabled && source!=null && sourceUi!=null && actionButton!=null)Render();}
    }
}
