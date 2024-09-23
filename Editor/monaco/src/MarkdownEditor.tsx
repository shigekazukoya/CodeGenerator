import React, { useRef, useEffect, useState } from 'react';
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
  const [MonacoEditor, setMonacoEditor] = useState<any>(null); // MonacoEditor の状態を保持

  useEffect(() => {
    // Monaco Editor の動的インポート
    import("@monaco-editor/react").then(({ default: LoadedEditor }) => {
      setMonacoEditor(() => LoadedEditor); // 読み込んだエディタを状態にセット
    });

    let monaco: typeof import("monaco-editor");

    import("monaco-editor").then((monacoEditor) => {
      monaco = monacoEditor;

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

      window.getText = () => {
        if (editorRef.current) {
          return editorRef.current.getValue();
        }
        return '';
      };

      const handleMessage = (event: MessageEvent) => {
        if (event.data.action === 'getText') {
          const text = window.getText();
          window.chrome?.webview?.postMessage({ action: 'getTextResult', text });
        }
      };

      window.addEventListener('message', handleMessage);

      return () => {
        window.removeEventListener('message', handleMessage);
      };
    });
  }, []);

  const handleEditorDidMount = (editor: any) => {
    editorRef.current = editor;
  };

  const handleEditorChange = (value: string | undefined) => {
    console.log(value);
  };

  return (
    <div>
      {/* MonacoEditorが読み込まれた後にエディタを表示 */}
      {MonacoEditor && (
        <MonacoEditor
          height="90vh"
          defaultLanguage="markdown"
          defaultValue=""
          options={{
            selectOnLineNumbers: true,
            roundedSelection: false,
            readOnly: false,
            cursorStyle: 'line',
            automaticLayout: true,
            theme: 'vs-dark',
            minimap: { enabled: false },
          }}
          onMount={handleEditorDidMount}
          onChange={handleEditorChange}
        />
      )}
    </div>
  );
};

export default MarkdownEditor;
