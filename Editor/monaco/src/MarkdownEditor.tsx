import React, { useRef, useEffect } from 'react';
import Editor, { EditorProps } from "@monaco-editor/react";
import * as monaco from "monaco-editor";

declare global {
  interface Window {
    setText: (text: string) => void;
    setLanguage: (language: string) => void;
    getText: () => string;
    chrome?: {
      webview?: {
        postMessage: (message: any) => void;
      };
    };
  }
}

const MarkdownEditor: React.FC = () => {
  const editorRef = useRef<any>(null);

  const editorOptions: EditorProps["options"] = {
    selectOnLineNumbers: true,
    roundedSelection: false,
    readOnly: false,
    cursorStyle: 'line',
    automaticLayout: true,
    theme: 'vs-dark',
    minimap: { enabled: false },
  };

  useEffect(() => {
    // テキストを設定する関数
    window.setText = (text: string) => {
      if (editorRef.current) {
        editorRef.current.setValue(text);
      }
    };
    window.setLanguage = (language: string) => {
      if (editorRef.current) {
        monaco.editor.setModelLanguage(editorRef.current.getModel(), language);
      }
    };

    // テキストを取得する関数
    window.getText = () => {
      if (editorRef.current) {
        console.log("取得");
        return editorRef.current.getValue();
      }
        console.log("とれていない");
      return '';
    };

    // C#からのメッセージを受け取るイベントリスナー
      const handleMessage = (event: MessageEvent) => {
          console.log("メッセージ受診",event.data);
      if (event.data.action === 'getText') {
          console.log("getText");
        const text = window.getText();
        window.chrome?.webview?.postMessage({ action: 'getTextResult', text: text });
        console.log("送った");
      }
    };

    window.addEventListener('message', handleMessage);

    return () => {
      window.removeEventListener('message', handleMessage);
    };
  }, []);

  const handleEditorDidMount = (editor: any) => {
    editorRef.current = editor;
  };

  const handleEditorChange = (value: string | undefined) => {
    console.log(value);
    // ここで必要に応じて追加の処理を行うことができます
  };

  return (
    <Editor
      height="90vh"
      defaultLanguage="markdown"
      defaultValue=""
      options={editorOptions}
      onMount={handleEditorDidMount}
      onChange={handleEditorChange}
    />
  );
};

export default MarkdownEditor;