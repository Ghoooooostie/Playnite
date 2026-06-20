# SwitchLocal Background Search Chain Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 `SwitchLocalMetadata` 在本地没有横图时，自动搜索官网图或公共图源，给 Switch 游戏补背景图。

**Architecture:** 保留现有本地 ROM 解包流程不动，只在背景图解析处新增一条自动搜索链。搜索链按“本地横图 -> 官网页 `og:image` -> SteamGridDB 页面 `og:image` -> 退回封面”的顺序工作，并把成功/失败结果写入插件用户目录缓存，避免重复联网。

**Tech Stack:** C# / .NET Framework 4.6.2 / Playnite SDK / `WebClient` / 正则 / 本地 JSON 缓存

---
