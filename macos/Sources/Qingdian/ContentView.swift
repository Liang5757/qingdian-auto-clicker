import SwiftUI
import QingdianCore

struct ContentView: View {
    @ObservedObject var model: AppModel
    private let blue = Color(red: 0.09, green: 0.44, blue: 0.96)
    var body: some View {
        ScrollView {
            VStack(spacing: 18) {
                VStack(spacing: 10) {
                    Text("让重复点击，更简单").font(.system(size: 28, weight: .bold))
                    Text("F9 开始 · F10 / Esc 停止").foregroundStyle(.secondary)
                }.padding(.top, 12)
                HStack {
                    Circle().fill(model.running ? Color.orange : Color.green).frame(width: 9, height: 9)
                    Text(model.status).fontWeight(.medium).fixedSize(horizontal: false, vertical: true)
                    Spacer()
                    VStack(alignment: .trailing, spacing: 4) {
                        Text(model.rounds.formatted()).font(.system(size: 30, weight: .bold, design: .rounded)).monospacedDigit()
                        Text("已完成轮数").font(.caption).foregroundStyle(.secondary)
                    }
                }.padding(20).card()
                VStack(spacing: 20) {
                    HStack(alignment: .top, spacing: 24) {
                        VStack(alignment: .leading, spacing: 9) {
                            Text("点击间隔").font(.headline)
                            HStack {
                                TextField("毫秒", value: $model.settings.interval, format: .number)
                                    .textFieldStyle(.roundedBorder).accessibilityLabel("点击间隔（毫秒）")
                                Stepper("", value: $model.settings.interval, in: 20...3_600_000, step: 10).labelsHidden()
                            }
                            Text("毫秒 · 最小 20 ms").font(.caption).foregroundStyle(.secondary)
                            Text("鼠标按键").font(.headline).padding(.top, 8)
                            Picker("鼠标按键", selection: $model.settings.button) {
                                Text("左键").tag(0); Text("右键").tag(1); Text("中键").tag(2)
                            }.pickerStyle(.segmented).labelsHidden()
                        }
                        VStack(alignment: .leading, spacing: 9) {
                            Text("点击轮数").font(.headline)
                            HStack {
                                TextField("轮数", value: $model.settings.limit, format: .number)
                                    .textFieldStyle(.roundedBorder).accessibilityLabel("点击轮数")
                                Stepper("", value: $model.settings.limit, in: 0...100_000_000).labelsHidden()
                            }
                            Text("0 表示持续运行").font(.caption).foregroundStyle(.secondary)
                            Text("点击方式").font(.headline).padding(.top, 8)
                            Picker("点击方式", selection: $model.settings.doubleClick) {
                                Text("单击").tag(false); Text("双击").tag(true)
                            }.pickerStyle(.segmented).labelsHidden()
                        }
                    }.disabled(model.running)
                    Label("始终点击鼠标当前位置", systemImage: "cursorarrow")
                        .foregroundStyle(.secondary).frame(maxWidth: .infinity, alignment: .leading)
                        .padding(12).background(Color.gray.opacity(0.06), in: RoundedRectangle(cornerRadius: 8))
                    HStack(spacing: 14) {
                        Button { model.start() } label: {
                            Label("开始连点     F9", systemImage: "play.fill").font(.headline).frame(maxWidth: .infinity).padding(.vertical, 10)
                        }.buttonStyle(.borderedProminent).tint(blue).disabled(model.running || !model.canStop || !model.permission)
                        Button { model.stop() } label: {
                            Label("停止     F10", systemImage: "stop.fill").font(.headline).frame(maxWidth: .infinity).padding(.vertical, 10)
                        }.buttonStyle(.bordered).tint(.red).disabled(!model.running)
                    }
                    Text("启动后预留 1 秒，移开鼠标即可").font(.caption).foregroundStyle(.secondary)
                }.padding(20).card()
                if !model.permission {
                    HStack {
                        Label("需要辅助功能权限才能发送点击", systemImage: "hand.raised")
                        Spacer(); Button("前往授权") { model.requestPermission() }
                    }.font(.callout).padding(12).background(Color.orange.opacity(0.1), in: RoundedRectangle(cornerRadius: 10))
                }
                VStack(alignment: .leading, spacing: 10) {
                    HStack(spacing: 24) {
                        Toggle("启动时隐藏到菜单栏", isOn: $model.settings.startHidden)
                        Toggle("登录时启动", isOn: Binding(get: { model.loginEnabled || model.loginPending }, set: { model.setLogin($0) }))
                    }.toggleStyle(.checkbox)
                    if model.loginPending { Text("登录项等待系统批准，请在系统设置中确认。").font(.caption).foregroundStyle(.orange) }
                    Text("关闭 / 最小化窗口后继续驻留；从菜单栏退出。\nMac 功能键可能需要同时按 Fn / 🌐。")
                        .font(.caption).foregroundStyle(.secondary)
                }.frame(maxWidth: .infinity, alignment: .leading)
                Divider()
                HStack {
                    Text(model.notice).font(.caption).foregroundStyle(.secondary)
                    Spacer(); Button("诊断日志 ↗") { model.openLog() }.buttonStyle(.link)
                }
            }.padding(24)
        }
        .frame(minWidth: 640, idealWidth: 680, minHeight: 700, idealHeight: 740)
        .background(Color(nsColor: .windowBackgroundColor))
        .onChange(of: model.settings) { _ in model.save() }
    }
}
private extension View {
    func card() -> some View {
        self.background(Color(nsColor: .controlBackgroundColor), in: RoundedRectangle(cornerRadius: 12))
            .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.gray.opacity(0.16), lineWidth: 1))
    }
}
