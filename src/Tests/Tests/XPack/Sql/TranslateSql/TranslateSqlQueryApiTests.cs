﻿using System;
using Elastic.Xunit.XunitPlumbing;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Tests.Core.ManagedElasticsearch.Clusters;
using Tests.Domain;
using Tests.Domain.Helpers;
using Tests.Framework;
using Tests.Framework.Integration;

namespace Tests.XPack.Sql.TranslateSql
{
	//[SkipVersion("<6.4.0", "")]
	public class TranslateSqlApiTests : ApiIntegrationTestBase<XPackCluster, ITranslateSqlResponse, ITranslateSqlRequest, TranslateSqlDescriptor, TranslateSqlRequest>
	{
		public TranslateSqlApiTests(XPackCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override LazyResponses ClientUsage() => this.Calls(
			fluent: (client, f) => client.TranslateSql(f),
			fluentAsync: (client, f) => client.TranslateSqlAsync(f),
			request: (client, r) => client.TranslateSql(r),
			requestAsync: (client, r) => client.TranslateSqlAsync(r)
		);

		protected override bool ExpectIsValid => true;
		protected override int ExpectStatusCode => 200;
		protected override HttpMethod HttpMethod => HttpMethod.POST;

		protected override string UrlPath => $"/_xpack/sql/translate";

		private static readonly string SqlQuery =
			$@"SELECT type, name, startedOn, numberOfCommits
FROM {TestValueHelper.ProjectsIndex}
WHERE type = '{Project.TypeName}'
ORDER BY numberOfContributors DESC";

		protected override object ExpectJson { get; } = new {
			query = SqlQuery,
			fetch_size = 5
		};

		protected override Func<TranslateSqlDescriptor, ITranslateSqlRequest> Fluent => d => d
			.Query(SqlQuery)
			.FetchSize(5)
		;

		protected override TranslateSqlRequest Initializer => new TranslateSqlRequest()
		{
			Query = SqlQuery,
			FetchSize = 5
		};

		protected override void ExpectResponse(ITranslateSqlResponse response)
		{
			var search = response.Result;
			search.Should().NotBeNull();
			search.Size.Should().HaveValue().And.Be(5);
			search.Source.Should().NotBeNull();
			search.Source.Match(b => b.Should().BeFalse(), f => f.Should().BeNull());
			search.DocValueFields.Should().NotBeNullOrEmpty().And.HaveCount(4)
				.And.Contain("type")
				.And.Contain("name")
				.And.Contain("startedOn")
				.And.Contain("numberOfCommits");

			search.Query.Should().NotBeNull();
			IQueryContainer q = search.Query;
			q.Term.Should().NotBeNull();
			q.Term.Value.Should().Be(TestValueHelper.ProjectsIndex);
			q.Term.Field.Should().Be("type");
		}
	}
}
