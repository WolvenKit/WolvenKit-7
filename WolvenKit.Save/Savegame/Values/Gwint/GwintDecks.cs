using WolvenKit.W3SavegameEditor.Core.Savegame.Attributes;

namespace WolvenKit.W3SavegameEditor.Core.Savegame.Values.Gwint
{
    public class GwintDecks
    {
        [CName("DeckCount")]
        public uint DeckCount { get; set; }

        public GwintDeck[] Decks { get; set; }
    }
}