# Armature Scale Copier ver.dennoko

Armature オブジェクトの子にある MA Scale adjust などのコンポーネントや Transform のスケール・位置情報を別の Armature オブジェクトにコピー・ペーストできるツールの、Armature Scale Copierの操作性を変更し、より効率的に使えるようにしたツールです。

## 機能

-   Armature オブジェクトの子のコンポーネント情報をコピー
-   Transform（位置、回転、スケール）情報のコピー
-   MA Scale adjust、MA Merge Armature、その他 ModularAvatar コンポーネントに対応
-   複数の Armature オブジェクト間での一括コピー・ペースト

## 使用方法

### Avatar Batch Copier（一括操作）

アバターの体型をいじったから、衣装側にも同じ調整を適用したい場合などに便利です。

1. Window → ShimotukiRieru → Avatar Batch Copier でツールウィンドウを開く
2. 検索範囲にアバターをドラッグ＆ドロップ
3. ペーストさせたい Armature オブジェクトをチェックボックスで選択
4. コピーを実行

### Single Copier

1. Window → ShimotukiRieru → Single Copier でツールウィンドウを開く
2. コピー元の Armature オブジェクトを選択
3. 「Copy Armature Data」ボタンでデータをコピー
4. コピー先の Armature オブジェクトを選択
5. 「Paste Armature Data」ボタンでデータをペースト

## 要件

-   Unity 2022.3 以降

## 動作確認済み

-   Unity 2022.3 以降
-   VRChat Avatars SDK 3.1.0 以降
-   VRChat SDK 3.1.0 以降

## インストール

VCC (VRChat Creator Companion) を使用してインストールしてください。

## ライセンス

MIT License
