import '../models/platform_feature_page.dart';
import '../models/user_role.dart';
import 'platform_api_client.dart';

class MockPlatformApiClient implements PlatformApiClient {
  @override
  Future<PlatformFeaturePage> getFeaturePage({
    required UserRole role,
    required String pageKey,
  }) async {
    await Future<void>.delayed(const Duration(milliseconds: 180));
    return switch (role) {
      UserRole.student => _studentPage(pageKey),
      UserRole.parent => _parentPage(pageKey),
    };
  }

  PlatformFeaturePage _studentPage(String pageKey) {
    return switch (pageKey) {
      'studentHomework' => _page(
          cards: [
            _card('待完成', '3', '今天 22:00 前'),
            _card('已提交', '12', '本周累计'),
            _card('待订正', '4', '错题需复盘'),
            _card('正确率', '84%', '较上周 +6%'),
          ],
          activities: [
            _activity('数学函数基础练习', '作业', '今天', '共 10 题，完成后进入错题本。', '待完成'),
            _activity('英语阅读理解 03', '作业', '今天', '完成后查看逐题讲解。', '待完成'),
          ],
          note: '学生端作业流后续接入扫描录入、在线提交和错题讲解。',
        ),
      'studentProgress' => _page(
          cards: [
            _card('本周完成度', '68%', '计划执行正常'),
            _card('数学', '92%', '优势稳定'),
            _card('英语', '73%', '阅读波动'),
            _card('物理', '81%', '改善中'),
          ],
          activities: [
            _activity('数学得分率提升', '趋势', '近 3 次', '函数题正确率从 76% 提升到 88%。', '提升'),
            _activity('学习计划调整', '计划', '本周', '英语阅读每天增加 10 分钟。', '已更新'),
          ],
          note: '学习进度页是学生自己的计划入口。',
        ),
      'studentPhotoQuestion' => _page(
          cards: [
            _card('拍照搜题', '待接入', '后续连接 OCR'),
            _card('讲解记录', '0', '当前为占位'),
            _card('错题沉淀', '0', '进入错题本'),
            _card('相关练习', '0', '按题型推荐'),
          ],
          activities: [
            _activity('上传题目照片', '入口', '随时', '后续接入 OCR、题目识别和分步讲解。', '占位'),
          ],
          note: '当前只保留拍照搜题入口，不实现 AI 解题逻辑。',
        ),
      _ => _page(
          cards: [
            _card('今日任务', '5', '2 项已完成'),
            _card('学习进度', '68%', '本周计划'),
            _card('薄弱学科', '英语', '阅读理解'),
            _card('连续学习', '9 天', '保持节奏'),
          ],
          activities: [
            _activity('数学函数专项', '今日计划', '20 分钟', '完成 8 道基础题和 2 道提高题。', '进行中'),
            _activity('英语阅读训练', '今日计划', '15 分钟', '完成阅读并订正错题。', '未开始'),
          ],
          note: '学生端只展示本人数据。',
        ),
    };
  }

  PlatformFeaturePage _parentPage(String pageKey) {
    return switch (pageKey) {
      'parentTrends' => _page(
          cards: [
            _card('总分趋势', '上升', '连续 2 次提升'),
            _card('年级排名', '25', '较上次 +8'),
            _card('最高学科', '数学', '得分率 91%'),
            _card('最低学科', '英语', '得分率 76%'),
          ],
          activities: [
            _activity('春季期中考试', '考试', '2026-04-20', '总分 451，年级排名 25。', '已完成'),
            _activity('三月月考', '考试', '2026-03-18', '总分 438，年级排名 33。', '已完成'),
          ],
          note: '家长端趋势页突出可行动建议，避免过度排名焦虑。',
        ),
      'parentReports' => _page(
          cards: [
            _card('周报告', '1', '本周已生成'),
            _card('月报告', '1', '本月已生成'),
            _card('教师反馈', '3', '待阅读 1 条'),
            _card('家庭建议', '4', '可执行事项'),
          ],
          activities: [
            _activity('第 12 周学习报告', '周报', '今天', '包含成绩趋势、作业完成和学习计划。', '新报告'),
            _activity('英语阅读专项反馈', '专项', '昨天', '建议家长关注阅读习惯。', '已读'),
          ],
          note: '报告正文后续接模板或 AI 生成。',
        ),
      'parentWellbeing' => _page(
          cards: [
            _card('学习压力', '中等', '作业量增加'),
            _card('情绪状态', '稳定', '无明显异常'),
            _card('睡眠提醒', '需关注', '两天低于 7 小时'),
            _card('沟通建议', '2 条', '面向家长'),
          ],
          activities: [
            _activity('压力波动提醒', '心理', '本周', '英语任务增加后压力偏高。', '关注'),
            _activity('家庭沟通建议', '建议', '今天', '先肯定执行情况，再讨论阅读训练。', '建议'),
          ],
          note: '心理情况页只做趋势和提醒，不做医学诊断。',
        ),
      _ => _page(
          cards: [
            _card('孩子排名', '25 / 486', '较上次 +8'),
            _card('作业完成', '92%', '本周完成率'),
            _card('优势学科', '数学', '稳定领先'),
            _card('需要关注', '英语', '阅读波动'),
          ],
          activities: [
            _activity('本周学习概况', '报告', '今天', '学习节奏稳定，数学优势明显。', '已更新'),
            _activity('教师建议', '沟通', '本周', '每天固定 15 分钟阅读训练。', '建议'),
          ],
          note: '家长端只展示绑定孩子的数据。',
        ),
    };
  }

  PlatformFeaturePage _page({
    required List<PlatformMetricCard> cards,
    required List<PlatformActivityItem> activities,
    required String note,
  }) {
    return PlatformFeaturePage(cards: cards, activities: activities, note: note);
  }

  PlatformMetricCard _card(String label, String value, String hint) {
    return PlatformMetricCard(label: label, value: value, hint: hint);
  }

  PlatformActivityItem _activity(String title, String category, String timeText, String detail, String status) {
    return PlatformActivityItem(
      title: title,
      category: category,
      timeText: timeText,
      detail: detail,
      status: status,
    );
  }
}
