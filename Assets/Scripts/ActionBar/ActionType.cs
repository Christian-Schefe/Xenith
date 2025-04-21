namespace ActionMenu
{
    public abstract class ActionType
    {
        public enum Variant
        {
            Button, Toggle
        }

        public abstract Variant Type { get; }

        public class Button : ActionType
        {
            public readonly string name;
            public readonly System.Action onClick;

            public override Variant Type => Variant.Button;

            public Button(string name, System.Action onClick)
            {
                this.name = name;
                this.onClick = onClick;
            }
        }

        public class Toggle : ActionType
        {
            public readonly string name;
            public readonly Box<bool> state;

            public override Variant Type => Variant.Toggle;

            public Toggle(string name, Box<bool> state)
            {
                this.name = name;
                this.state = state;
            }
        }
    }
}