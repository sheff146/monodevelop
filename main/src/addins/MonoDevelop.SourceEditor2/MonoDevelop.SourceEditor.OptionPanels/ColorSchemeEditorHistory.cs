//
// ColorSchemeEditorHistory.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Xwt.Drawing;
using Mono.TextEditor.Highlighting;
using Xwt;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public class ColorSchemeEditorHistory
	{
		readonly Stack<StyleCommand> undoStack = new Stack<StyleCommand> ();
		readonly Stack<StyleCommand> redoStack = new Stack<StyleCommand> ();
		readonly TreeView treeView;
		readonly IDataField<object> dataField;
		readonly IDataField<ColorScheme.PropertyDecsription> propertyField;

		public event EventHandler CanUndoRedoChanged;

		public ColorSchemeEditorHistory (TreeView treeView, IDataField<object> dataField, IDataField<ColorScheme.PropertyDecsription> propertyField)
		{
			this.treeView = treeView;
			this.dataField = dataField;
			this.propertyField = propertyField;
		}

		public void AddCommand (StyleCommand command)
		{
			redoStack.Clear ();
			undoStack.Push (command);
			var navigator = GetNavigator (command);
			command.Redo (navigator, dataField);

			if (CanUndoRedoChanged != null)
				CanUndoRedoChanged (this, new EventArgs ());
		}

		private TreeNavigator GetNavigator (StyleCommand command)
		{
			var treeStore = treeView.DataSource as TreeStore;
			return treeStore == null
				? null
				: treeStore.GetNodeFromStyleName (command.StyleName, propertyField);
		}

		public bool CanUndo{ get { return undoStack.Count > 0; } }

		public bool CanRedo{ get { return redoStack.Count > 0; } }

		public void Undo ()
		{
			if (!CanUndo)
				return;
			var command = undoStack.Pop ();
			var navigator = GetNavigator (command);
			command.Undo (navigator, dataField);
			redoStack.Push (command);
			SelectAndScroll (navigator.CurrentPosition);

			if (CanUndoRedoChanged != null)
				CanUndoRedoChanged (this, new EventArgs ());
		}

		private void SelectAndScroll (TreePosition position)
		{
			treeView.SelectRow (position);
			treeView.ScrollToRow (position);
		}

		public void Redo ()
		{
			if (!CanRedo)
				return;
			var command = redoStack.Pop ();
			var navigator = GetNavigator (command);
			undoStack.Push (command);
			command.Redo (navigator, dataField);
			SelectAndScroll (navigator.CurrentPosition);

			if (CanUndoRedoChanged != null)
				CanUndoRedoChanged (this, new EventArgs ());
		}
	}

	public abstract class StyleCommand
	{
		public string StyleName{ get; private set; }

		protected StyleCommand (string styleName)
		{
			this.StyleName = styleName;
		}

		public abstract void Undo (TreeNavigator navigator, IDataField<object> dataField);

		public abstract void Redo (TreeNavigator navigator, IDataField<object> dataField);
	}

	public class ChangeChunkStyleCommand:StyleCommand
	{
		ChunkStyle oldStyle;
		ChunkStyle newStyle;

		public ChangeChunkStyleCommand (ChunkStyle oldStyle, ChunkStyle newStyle, string styleName)
			: base (styleName)
		{
			this.oldStyle = oldStyle;
			this.newStyle = newStyle;
		}

		public override void Undo (TreeNavigator navigator, IDataField<object> dataField)
		{
			navigator.SetValue (dataField, oldStyle);
		}

		public override void Redo (TreeNavigator navigator, IDataField<object> dataField)
		{
			navigator.SetValue (dataField, newStyle);
		}
	}

	public class ChangeAmbientColorCommand:StyleCommand
	{
		AmbientColor oldStyle;
		AmbientColor newStyle;

		public ChangeAmbientColorCommand (AmbientColor oldStyle, AmbientColor newStyle, string styleName)
			: base (styleName)
		{
			this.oldStyle = oldStyle;
			this.newStyle = newStyle;
		}

		public override void Undo (TreeNavigator navigator, IDataField<object> dataField)
		{
			navigator.SetValue (dataField, oldStyle);
		}

		public override void Redo (TreeNavigator navigator, IDataField<object> dataField)
		{
			navigator.SetValue (dataField, newStyle);
		}
	}
}
