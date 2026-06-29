class PlatformFeaturePage {
  const PlatformFeaturePage({
    required this.cards,
    required this.activities,
    required this.note,
  });

  factory PlatformFeaturePage.fromJson(Map<String, dynamic> json) {
    return PlatformFeaturePage(
      cards: (json['cards'] as List<dynamic>? ?? [])
          .map((item) => PlatformMetricCard.fromJson(item as Map<String, dynamic>))
          .toList(),
      activities: (json['activities'] as List<dynamic>? ?? [])
          .map((item) => PlatformActivityItem.fromJson(item as Map<String, dynamic>))
          .toList(),
      note: json['note'] as String? ?? '',
    );
  }

  final List<PlatformMetricCard> cards;
  final List<PlatformActivityItem> activities;
  final String note;
}

class PlatformMetricCard {
  const PlatformMetricCard({
    required this.label,
    required this.value,
    required this.hint,
  });

  factory PlatformMetricCard.fromJson(Map<String, dynamic> json) {
    return PlatformMetricCard(
      label: json['label'] as String? ?? '',
      value: json['value'] as String? ?? '',
      hint: json['hint'] as String? ?? '',
    );
  }

  final String label;
  final String value;
  final String hint;
}

class PlatformActivityItem {
  const PlatformActivityItem({
    required this.title,
    required this.category,
    required this.timeText,
    required this.detail,
    required this.status,
  });

  factory PlatformActivityItem.fromJson(Map<String, dynamic> json) {
    return PlatformActivityItem(
      title: json['title'] as String? ?? '',
      category: json['category'] as String? ?? '',
      timeText: json['timeText'] as String? ?? '',
      detail: json['detail'] as String? ?? '',
      status: json['status'] as String? ?? '',
    );
  }

  final String title;
  final String category;
  final String timeText;
  final String detail;
  final String status;
}
