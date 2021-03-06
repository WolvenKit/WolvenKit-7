using WolvenKit.W3SavegameEditor.Core.Savegame.Attributes;

namespace WolvenKit.W3SavegameEditor.Core.Savegame.Values.Quests
{
    [CSerializable("quest")]
    public class Quest
    {
        [CName("fileName")]
        public string FileName { get; set; }

        [CName("questThread")]
        public QuestThread QuestThread { get; set; }

    }
}