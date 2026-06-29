import 'dart:convert';
import 'dart:io';

import '../models/platform_feature_page.dart';
import '../models/user_role.dart';
import 'platform_api_client.dart';

class HttpPlatformApiClient implements PlatformApiClient {
  HttpPlatformApiClient({
    this.baseUrl = const String.fromEnvironment(
      'TOSAI_API_BASE_URL',
      defaultValue: 'http://10.0.2.2:5088',
    ),
  });

  final String baseUrl;

  @override
  Future<PlatformFeaturePage> getFeaturePage({
    required UserRole role,
    required String pageKey,
  }) async {
    final uri = Uri.parse('${baseUrl.replaceAll(RegExp(r'/+$'), '')}/api/platform/${role.apiName}/$pageKey');
    final client = HttpClient()..connectionTimeout = const Duration(seconds: 5);

    try {
      final request = await client.getUrl(uri);
      request.headers.set(HttpHeaders.acceptHeader, 'application/json');
      final response = await request.close().timeout(const Duration(seconds: 8));
      final body = await response.transform(utf8.decoder).join();

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw HttpException('API returned ${response.statusCode}: $body', uri: uri);
      }

      return PlatformFeaturePage.fromJson(jsonDecode(body) as Map<String, dynamic>);
    } finally {
      client.close(force: true);
    }
  }
}
