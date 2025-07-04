// SPDX-FileCopyrightText: 2020 Julian Giebel <j.giebel@netrocks.info>
// SPDX-FileCopyrightText: 2020 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2020 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Text.RegularExpressions;
using Content.Shared.Configurable;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Client.Configurable.UI
{
    public sealed class ConfigurationBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ConfigurationMenu? _menu;

        public ConfigurationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ConfigurationMenu>();
            _menu.OnConfiguration += SendConfiguration;
            if (EntMan.TryGetComponent(Owner, out ConfigurationComponent? component))
                Refresh((Owner, component));
        }

        public void Refresh(Entity<ConfigurationComponent> entity)
        {
            if (_menu == null)
                return;

            _menu.Column.Children.Clear();
            _menu.Inputs.Clear();

            foreach (var field in entity.Comp.Config)
            {
                var label = new Label
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    Name = field.Key,
                    Text = field.Key + ":",
                    VerticalAlignment = Control.VAlignment.Center,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .2f,
                    MinSize = new Vector2(60, 0)
                };

                var input = new LineEdit
                {
                    Name = field.Key + "-input",
                    Text = field.Value ?? "",
                    IsValid = _menu.Validate,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .8f
                };

                _menu.Inputs.Add((field.Key, input));

                var row = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal
                };

                ConfigurationMenu.CopyProperties(_menu.Row, row);

                row.AddChild(label);
                row.AddChild(input);
                _menu.Column.AddChild(row);
            }
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (_menu == null)
                return;

            if (message is ValidationUpdateMessage msg)
            {
                _menu.Validation = new Regex(msg.ValidationString, RegexOptions.Compiled);
            }
        }

        public void SendConfiguration(Dictionary<string, string> config)
        {
            SendMessage(new ConfigurationUpdatedMessage(config));
        }
    }
}