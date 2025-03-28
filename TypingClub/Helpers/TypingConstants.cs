namespace TypingClub.Helpers
{
    public static class TypingConstants
    {
        public static readonly string[] Paragraphs = new[]
        {
            // Original paragraphs.
            "This is a longer paragraph for the typing race game. It includes more text to make the game challenging and fun. Users will need to type this entire paragraph correctly to win the race. The quick brown fox jumps over the lazy dog, and the race continues with more sentences to test their typing speed and accuracy.",
            "Another extended paragraph for the typing race. This one is designed to be even longer, providing a greater challenge for participants. Typing accurately and quickly is key to winning. Practice makes perfect, and with each race, users can improve their skills. The race is on, and only the fastest typist will emerge victorious.",

            // Funny paragraphs.
            "Life is a carnival of mishaps, where even the simplest typo can spark a burst of laughter. Imagine a keyboard that chuckles with every keystroke. In this race, speed meets humor, turning each mistake into a moment of joy.",
            "Sometimes the best part of typing is the unexpected words that come out when your fingers decide to have a mind of their own. Embrace the quirky chaos and let the typos remind you not to take things too seriously.",
            "Picture a world where every error writes its own punchline. As you race against the clock, every misplaced comma or rogue letter becomes part of a hilarious story unfolding right before your eyes.",
            "If your keyboard had a personality, it might tease you about every mistake. Each key press is a mini adventure in mischief and mayhem, turning the race into a comedic journey.",
            "In this game, every typo is an opportunity for laughter. Embrace the absurdity of a wandering finger on the keyboard and let each error create a quirky masterpiece.",

            // Literary / Inspired paragraphs.
            "It was the best of times, it was the worst of times. Inspired by classic literature, this passage evokes the grandeur of timeless works, challenging participants to channel their inner author as they race against the clock.",
            "As the pen scratches the paper in an endless dance of words, so do your fingers tap in a rhythmic race. Embrace the literary challenge as you traverse a passage rich with history and style.",
            "Amidst the gentle hum of whispered legends and the rustle of ancient pages, lies a story waiting to be typed. Let your words flow as gracefully as a sonnet from a bygone era.",
            "In a realm of words and dreams, every letter tells a story. Let your fingers weave an epic saga as you transform keystrokes into timeless adventures.",
            "As the rhythm of the keys echoes the beats of a timeless verse, immerse yourself in the dance of letters and let each word paint a vivid picture.",

            // Motivational / Historical / Tech-themed paragraphs.
            "Your keystrokes are like brushstrokes on the canvas of success. With every word typed, you're creating a masterpiece of determination and skill.",
            "Imagine the great scribes of old, etching their thoughts onto parchment with quills. Now, you carry that legacy forward with every keystroke you make.",
            "In the digital realm where speed meets precision, every keystroke is a command and every error a chance to debug your strategy. Embrace the efficiency of modern typing.",
            "Each tap on your keyboard is a step towards excellence. Let the rhythm of your fingers propel you towards achieving greatness in this race.",
            "Channel the spirit of legendary authors and pioneers. As you type, feel the weight of history and the promise of future innovation."
        };

        public static readonly List<string> DefaultAvailableIcons = new List<string>
        {
            "image1.png", "image6.png",
            "image7.png", "image8.png", "image9.png", "image10.png",
            "image11.png", "image12.png", "image13.png", "image14.png",
            "image15.png", "image16.png", "image17.png", "image18.png",
            "image19.png",
        };

        public static string GetRandomParagraph()
        {
            var random = new Random();
            return Paragraphs[random.Next(Paragraphs.Length)];
        }
    }

}
