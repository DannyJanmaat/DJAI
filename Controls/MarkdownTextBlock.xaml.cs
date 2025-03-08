using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Text.RegularExpressions;

namespace DJAI.Controls
{
    public sealed partial class MarkdownTextBlock : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MarkdownTextBlock),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MarkdownTextBlock)d;
            control.RenderMarkdown((string)e.NewValue);
        }

        public MarkdownTextBlock()
        {
            this.InitializeComponent();
        }

        private void RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                TextBlock.Blocks.Clear();
                return;
            }

            // Eenvoudige verwerking - in een echte app zou je een volledige Markdown parser gebruiken
            TextBlock.Blocks.Clear();

            // Eenvoudige code blocks markering
            var formattedText = markdown
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            // Maak een eenvoudige paragraph voor de tekst
            var paragraph = new Paragraph();
            var run = new Run { Text = formattedText };
            paragraph.Inlines.Add(run);
            TextBlock.Blocks.Add(paragraph);
        }
    }
}