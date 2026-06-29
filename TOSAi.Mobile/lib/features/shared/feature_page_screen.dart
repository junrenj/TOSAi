import 'package:flutter/material.dart';

import '../../core/api/platform_api_client.dart';
import '../../core/models/platform_feature_page.dart';
import '../../core/models/user_role.dart';

class FeaturePageScreen extends StatefulWidget {
  const FeaturePageScreen({
    required this.apiClient,
    required this.role,
    required this.pageKey,
    super.key,
  });

  final PlatformApiClient apiClient;
  final UserRole role;
  final String pageKey;

  @override
  State<FeaturePageScreen> createState() => _FeaturePageScreenState();
}

class _FeaturePageScreenState extends State<FeaturePageScreen> {
  late Future<PlatformFeaturePage> _pageFuture;

  @override
  void initState() {
    super.initState();
    _pageFuture = _loadPage();
  }

  Future<PlatformFeaturePage> _loadPage() {
    return widget.apiClient.getFeaturePage(role: widget.role, pageKey: widget.pageKey);
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: () async {
        setState(() => _pageFuture = _loadPage());
        await _pageFuture;
      },
      child: FutureBuilder<PlatformFeaturePage>(
        future: _pageFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const _LoadingView();
          }

          if (snapshot.hasError) {
            return _ErrorView(
              message: snapshot.error.toString(),
              onRetry: () => setState(() => _pageFuture = _loadPage()),
            );
          }

          final page = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 24),
            children: [
              _MetricGrid(cards: page.cards),
              const SizedBox(height: 18),
              Text('最新动态', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(height: 12),
              for (final item in page.activities) ...[
                _ActivityTile(item: item),
                const SizedBox(height: 10),
              ],
              const SizedBox(height: 8),
              _NoteCard(note: page.note),
            ],
          );
        },
      ),
    );
  }
}

class _MetricGrid extends StatelessWidget {
  const _MetricGrid({required this.cards});

  final List<PlatformMetricCard> cards;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 560;
        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: cards.length,
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: isWide ? 4 : 2,
            crossAxisSpacing: 10,
            mainAxisSpacing: 10,
            childAspectRatio: isWide ? 1.25 : 1.18,
          ),
          itemBuilder: (context, index) => _MetricCard(card: cards[index]),
        );
      },
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({required this.card});

  final PlatformMetricCard card;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(card.label, maxLines: 1, overflow: TextOverflow.ellipsis, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black54)),
            const Spacer(),
            FittedBox(
              fit: BoxFit.scaleDown,
              alignment: Alignment.centerLeft,
              child: Text(card.value, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900)),
            ),
            const SizedBox(height: 6),
            Text(card.hint, maxLines: 2, overflow: TextOverflow.ellipsis, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black54)),
          ],
        ),
      ),
    );
  }
}

class _ActivityTile extends StatelessWidget {
  const _ActivityTile({required this.item});

  final PlatformActivityItem item;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.primaryContainer,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(_iconForCategory(item.category), color: Theme.of(context).colorScheme.primary, size: 22),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(item.title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
                  const SizedBox(height: 5),
                  Text(item.detail, style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.black67)),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    runSpacing: 6,
                    children: [
                      _ChipLabel(text: item.category),
                      _ChipLabel(text: item.timeText),
                      _ChipLabel(text: item.status, isStrong: true),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  IconData _iconForCategory(String category) {
    if (category.contains('作业')) return Icons.assignment_outlined;
    if (category.contains('考试')) return Icons.fact_check_outlined;
    if (category.contains('报告')) return Icons.article_outlined;
    if (category.contains('心理')) return Icons.favorite_border_outlined;
    if (category.contains('计划')) return Icons.event_note_outlined;
    return Icons.notifications_none_outlined;
  }
}

class _ChipLabel extends StatelessWidget {
  const _ChipLabel({required this.text, this.isStrong = false});

  final String text;
  final bool isStrong;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: isStrong ? Theme.of(context).colorScheme.primaryContainer : const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        text,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: isStrong ? Theme.of(context).colorScheme.primary : Colors.black54,
              fontWeight: isStrong ? FontWeight.w800 : FontWeight.w600,
            ),
      ),
    );
  }
}

class _NoteCard extends StatelessWidget {
  const _NoteCard({required this.note});

  final String note;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: Theme.of(context).colorScheme.primaryContainer.withOpacity(0.5),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.info_outline, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: 10),
            Expanded(child: Text(note, style: Theme.of(context).textTheme.bodyMedium)),
          ],
        ),
      ),
    );
  }
}

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(child: CircularProgressIndicator());
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('加载失败', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                const SizedBox(height: 8),
                Text(message, style: const TextStyle(color: Colors.black54)),
                const SizedBox(height: 16),
                FilledButton.icon(
                  onPressed: onRetry,
                  icon: const Icon(Icons.refresh),
                  label: const Text('重试'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

